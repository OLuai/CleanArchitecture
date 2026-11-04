#!/usr/bin/env bash
# Prépare un hôte Ubuntu pour Deploy-Preprod.ps1 : Docker, un utilisateur de déploiement dédié
# (sans mot de passe, sans sudo, membre du groupe docker, connexion par clé SSH seulement) et le
# répertoire de l'environnement sous /opt, avec un .env à renseigner.
#
# Idempotent : relançable pour ajouter une clé ou réparer des droits. À lancer en root, une fois :
#
#   sudo ./setup-host.sh "ssh-ed25519 AAAA... pc-de-dev"        # clé publique du PC qui déploie
#   sudo DEPLOY_USER=deploy ENV_DIR=/opt/cleanarchitecture-preprod ./setup-host.sh "ssh-ed25519 AAAA..."
#
# Le groupe docker équivaut à root sur l'hôte : ce compte ne sert qu'à déployer, depuis un PC
# dont la clé privée est protégée.
set -euo pipefail

DEPLOY_USER="${DEPLOY_USER:-deploy}"
ENV_DIR="${ENV_DIR:-/opt/cleanarchitecture-preprod}"
PUBKEY="${1:-}"

if [[ $EUID -ne 0 ]]; then
  echo "À lancer en root : sudo $0 '<clé publique ssh>'" >&2
  exit 1
fi
if [[ -z "$PUBKEY" ]]; then
  echo "Usage : sudo $0 '<clé publique ssh du PC qui déploie>'" >&2
  exit 1
fi

echo "==> Docker"
if ! command -v docker >/dev/null 2>&1; then
  apt-get update
  apt-get install -y docker.io docker-compose-v2
fi
if ! docker compose version >/dev/null 2>&1; then
  echo "Le plugin docker compose manque : apt-get install docker-compose-v2" >&2
  exit 1
fi
systemctl enable --now docker

echo "==> Utilisateur $DEPLOY_USER"
if ! id "$DEPLOY_USER" >/dev/null 2>&1; then
  # Sans mot de passe : aucune connexion possible autrement que par la clé installée ci-dessous.
  adduser --disabled-password --gecos "CleanArchitecture deployment" "$DEPLOY_USER"
fi
usermod -aG docker "$DEPLOY_USER"

home_dir="$(getent passwd "$DEPLOY_USER" | cut -d: -f6)"
install -d -m 700 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$home_dir/.ssh"
authorized_keys="$home_dir/.ssh/authorized_keys"
touch "$authorized_keys"
if ! grep -qxF "$PUBKEY" "$authorized_keys"; then
  echo "$PUBKEY" >> "$authorized_keys"
fi
chmod 600 "$authorized_keys"
chown "$DEPLOY_USER:$DEPLOY_USER" "$authorized_keys"

echo "==> Répertoire $ENV_DIR"
install -d -m 750 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$ENV_DIR"
if [[ ! -f "$ENV_DIR/.env" ]]; then
  example="$(dirname "$(readlink -f "$0")")/.env.example"
  if [[ -f "$example" ]]; then
    # 600 : le fichier porte les mots de passe de la base et du relais SMTP.
    install -m 600 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$example" "$ENV_DIR/.env"
    echo "    .env créé depuis .env.example : à renseigner avant le premier déploiement."
  else
    echo "    Pas de .env.example à côté du script : créer $ENV_DIR/.env à la main."
  fi
fi

cat <<EOF

Hôte prêt.
  Utilisateur : $DEPLOY_USER (groupe docker, clé SSH installée)
  Environnement : $ENV_DIR ($(test -f "$ENV_DIR/.env" && echo ".env présent" || echo ".env à créer"))

Depuis le PC de développement :
  .\\deploy\\Deploy-Preprod.ps1 -Target $DEPLOY_USER@$(hostname -I 2>/dev/null | awk '{print $1}')$( [[ "$ENV_DIR" != /opt/cleanarchitecture-preprod ]] && echo " -RemoteDir $ENV_DIR" )
EOF

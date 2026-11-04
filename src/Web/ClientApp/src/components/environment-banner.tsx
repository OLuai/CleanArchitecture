import { useEnvironment } from '@/lib/environment';

/**
 * Environment banner (Staging / Development) shown at the top of every page, including the
 * authentication screens. Deliberately saturated so it can never be mistaken for production.
 * No dismiss button: the marker must stay visible for the whole session.
 */
export function EnvironmentBanner() {
  const { environment, isProduction } = useEnvironment();

  if (isProduction || environment === 'Unknown') return null;

  const label = environment === 'Staging' ? 'STAGING' : 'DÉVELOPPEMENT';

  return (
    <div className="bg-orange-500 px-4 py-1 text-center text-xs font-semibold tracking-wide text-white uppercase">
      Environnement de test — {label} — Les données saisies ici ne sont pas réelles.
    </div>
  );
}

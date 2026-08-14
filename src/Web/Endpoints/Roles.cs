using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using CleanArchitecture.Application.RoleManagement;
using CleanArchitecture.Application.RoleManagement.Commands.CreateRole;
using CleanArchitecture.Application.RoleManagement.Commands.DeleteRole;
using CleanArchitecture.Application.RoleManagement.Commands.SetRolePermissions;
using CleanArchitecture.Application.RoleManagement.Queries.GetPermissions;
using CleanArchitecture.Application.RoleManagement.Queries.GetRoles;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Web.Endpoints;

public class Roles : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetRoles)
            .RequireAuthorization($"Permission:{Permissions.Roles.View}");
        groupBuilder.MapGet(GetPermissions, "permissions")
            .RequireAuthorization($"Permission:{Permissions.Roles.View}");
        groupBuilder.MapPost(CreateRole)
            .RequireAuthorization($"Permission:{Permissions.Roles.Manage}");
        groupBuilder.MapPut(SetRolePermissions, "{id}/permissions")
            .RequireAuthorization($"Permission:{Permissions.Roles.Manage}");
        groupBuilder.MapDelete(DeleteRole, "{id}")
            .RequireAuthorization($"Permission:{Permissions.Roles.Manage}");
    }

    [EndpointSummary("List roles")]
    [EndpointDescription("Returns every role with the permissions it grants and its member count.")]
    public static async Task<Ok<IReadOnlyList<RoleDto>>> GetRoles(ISender sender)
        => TypedResults.Ok(await sender.Send(new GetRolesQuery()));

    [EndpointSummary("List permissions")]
    [EndpointDescription("Returns the permission catalogue grouped by area, for the role editor.")]
    public static async Task<Ok<IReadOnlyList<PermissionGroupDto>>> GetPermissions(ISender sender)
        => TypedResults.Ok(await sender.Send(new GetPermissionsQuery()));

    [EndpointSummary("Create a role")]
    public static async Task<Created<string>> CreateRole(ISender sender, [FromBody] CreateRoleCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/api/Roles/{id}", id);
    }

    [EndpointSummary("Set a role's permissions")]
    [EndpointDescription("Replaces the permissions the role grants. Members' sessions are refreshed so they pick up the change.")]
    public static async Task<NoContent> SetRolePermissions(
        ISender sender, string id, [FromBody] SetRolePermissionsCommand command)
    {
        await sender.Send(command with { RoleId = id });

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete a role")]
    [EndpointDescription("Refuses to delete a built-in role or one that still has members.")]
    public static async Task<NoContent> DeleteRole(ISender sender, string id)
    {
        await sender.Send(new DeleteRoleCommand(id));

        return TypedResults.NoContent();
    }
}

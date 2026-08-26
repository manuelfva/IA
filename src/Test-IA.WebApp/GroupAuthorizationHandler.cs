using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA.WebApp;

/// <summary>
/// Authorization handler that checks if the authenticated user is a member of the required Active Directory group.
/// </summary>
public class GroupAuthorizationHandler : AuthorizationHandler<GroupAuthorizationRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GroupAuthorizationHandler> _logger;
    private readonly AuthorizationSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory for resolving scoped services.</param>
    /// <param name="logger">Logger for authorization handler events.</param>
    /// <param name="settings">The authorization settings.</param>
    public GroupAuthorizationHandler(IServiceScopeFactory scopeFactory, ILogger<GroupAuthorizationHandler> logger, IOptions<AuthorizationSettings> settings)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GroupAuthorizationRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Fail();
            return;
        }

        // Create a scope to resolve scoped services (IUserGroupAuthorizationService)
        using var scope = _scopeFactory.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IUserGroupAuthorizationService>();

        try
        {
            var isMember = authorizationService.IsMemberOfGroup(requirement.RequiredGroup);
            if (isMember)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
        catch (MissingGroupException ex)
        {
            // Log the error and fail authorization
            _logger.LogError(ex, "Authorization group '{GroupName}' does not exist in Active Directory. Authorization failed.", requirement.RequiredGroup);
            context.Fail();
        }
        catch (DomainException ex)
        {
            // Log the error and fail authorization
            _logger.LogError(ex, "Domain error while checking group membership for '{GroupName}'. Authorization failed.", requirement.RequiredGroup);
            context.Fail();
        }
    }
}

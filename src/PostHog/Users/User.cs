using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;

public class User
{
    [JsonPropertyName("date_joined")]
    public DateTime DateJoined { get; set; }

    [JsonPropertyName("uuid")]
    public Guid Uuid { get; set; }

    [JsonPropertyName("distinct_id")]
    public string? DistinctId { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("pending_email")]
    public string? PendingEmail { get; set; }

    [JsonPropertyName("is_email_verified")]
    public bool IsEmailVerified { get; set; }

    [JsonPropertyName("notification_settings")]
    public NotificationSettings? NotificationSettings { get; set; }

    [JsonPropertyName("anonymize_data")]
    public bool AnonymizeData { get; set; }

    [JsonPropertyName("toolbar_mode")]
    public string? ToolbarMode { get; set; }

    [JsonPropertyName("has_password")]
    public bool HasPassword { get; set; }

    [JsonPropertyName("is_staff")]
    public bool IsStaff { get; set; }

    [JsonPropertyName("is_impersonated")]
    public bool IsImpersonated { get; set; }

    [JsonPropertyName("is_impersonated_until")]
    public DateTime? IsImpersonatedUntil { get; set; }

    [JsonPropertyName("sensitive_session_expires_at")]
    public DateTime? SensitiveSessionExpiresAt { get; set; }

    [JsonPropertyName("team")]
    public Team? Team { get; set; }

    [JsonPropertyName("organization")]
    public Organization? Organization { get; set; }

    [JsonPropertyName("organizations")]
    public List<Organization> Organizations { get; set; } = new();

    [JsonPropertyName("events_column_config")]
    public EventsColumnConfig? EventsColumnConfig { get; set; }

    [JsonPropertyName("is_2fa_enabled")]
    public bool IsTwoFactorAuthenticationEnabled { get; set; }

    [JsonPropertyName("has_social_auth")]
    public bool HasSocialAuth { get; set; }

    [JsonPropertyName("has_seen_product_intro_for")]
    public object? HasSeenProductIntroFor { get; set; }

    [JsonPropertyName("scene_personalisation")]
    public List<object> ScenePersonalisation { get; set; } = new();

    [JsonPropertyName("theme_mode")]
    public object? ThemeMode { get; set; }

    [JsonPropertyName("hedgehog_config")]
    public object? HedgehogConfig { get; set; }
}

public class NotificationSettings
{
    [JsonPropertyName("plugin_disabled")]
    public bool PluginDisabled { get; set; }
}

public class Team
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uuid")]
    public Guid Uuid { get; set; }

    [JsonPropertyName("organization")]
    public Guid Organization { get; set; }

    [JsonPropertyName("project_id")]
    public int ProjectId { get; set; }

    [JsonPropertyName("api_token")]
    public string? ApiToken { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("completed_snippet_onboarding")]
    public bool CompletedSnippetOnboarding { get; set; }

    [JsonPropertyName("has_completed_onboarding_for")]
    public HasCompletedOnboardingFor? HasCompletedOnboardingFor { get; set; }

    [JsonPropertyName("ingested_event")]
    public bool IngestedEvent { get; set; }

    [JsonPropertyName("is_demo")]
    public bool IsDemo { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("access_control")]
    public bool AccessControl { get; set; }
}

public class HasCompletedOnboardingFor
{
    [JsonPropertyName("product_analytics")]
    public bool ProductAnalytics { get; set; }
}

public class Organization
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("logo_media_id")]
    public object? LogoMediaId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("membership_level")]
    public int MembershipLevel { get; set; }

    [JsonPropertyName("plugins_access_level")]
    public int PluginsAccessLevel { get; set; }

    [JsonPropertyName("teams")]
    public List<Team> Teams { get; set; } = new();

    [JsonPropertyName("projects")]
    public List<Project> Projects { get; set; } = new();

    [JsonPropertyName("available_product_features")]
    public List<AvailableProductFeature> AvailableProductFeatures { get; set; } = new();

    [JsonPropertyName("is_member_join_email_enabled")]
    public bool IsMemberJoinEmailEnabled { get; set; }

    [JsonPropertyName("metadata")]
    public Metadata? Metadata { get; set; }

    [JsonPropertyName("customer_id")]
    public object? CustomerId { get; set; }

    [JsonPropertyName("enforce_2fa")]
    public object? EnforceTwoFactorAuthentication { get; set; }

    [JsonPropertyName("member_count")]
    public int MemberCount { get; set; }
}

public class Project
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class AvailableProductFeature
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("note")]
    public object? Note { get; set; }

    [JsonPropertyName("unit")]
    public object? Unit { get; set; }

    [JsonPropertyName("limit")]
    public object? Limit { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("is_plan_default")]
    public bool IsPlanDefault { get; set; }

    [JsonPropertyName("entitlement_only")]
    public bool EntitlementOnly { get; set; }
}

public class Metadata
{
    [JsonPropertyName("instance_tag")]
    public string? InstanceTag { get; set; }
}

public class EventsColumnConfig
{
    [JsonPropertyName("active")]
    public string? Active { get; set; }
}
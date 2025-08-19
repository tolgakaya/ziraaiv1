using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class SponsorCustomization
    {
        public int Id { get; set; }
        public int SponsorId { get; set; }
        public BrandingConfiguration Branding { get; set; }
        public ThemeConfiguration Theme { get; set; }
        public WorkflowConfiguration Workflow { get; set; }
        public List<CustomField> CustomFields { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class SponsorCustomizationUpdate
    {
        public BrandingConfiguration Branding { get; set; }
        public ThemeConfiguration Theme { get; set; }
        public WorkflowConfiguration Workflow { get; set; }
        public List<CustomField> CustomFields { get; set; }
    }

    public class BrandingConfiguration
    {
        public string CompanyName { get; set; }
        public string LogoUrl { get; set; }
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string AccentColor { get; set; }
        public string Website { get; set; }
        public string ContactEmail { get; set; }
        public string Description { get; set; }
    }

    public class ThemeConfiguration
    {
        public string ThemeName { get; set; }
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
        public string ButtonColor { get; set; }
        public string FontFamily { get; set; }
        public int FontSize { get; set; }
        public Dictionary<string, object> CustomStyles { get; set; }
    }

    public class WorkflowConfiguration
    {
        public string WorkflowName { get; set; }
        public List<WorkflowStep> Steps { get; set; }
        public Dictionary<string, object> Settings { get; set; }
        public bool IsActive { get; set; }
    }

    public class WorkflowStep
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public bool IsRequired { get; set; }
    }

    public class CustomField
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public object DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public Dictionary<string, object> Validation { get; set; }
        public List<string> Options { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CustomTheme
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ThemeConfiguration Configuration { get; set; }
        public bool IsBuiltIn { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class WorkflowTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public WorkflowConfiguration Configuration { get; set; }
        public bool IsBuiltIn { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class UsageMetrics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalCustomizations { get; set; }
        public int ThemeUsage { get; set; }
        public int WorkflowUsage { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class UserEngagementMetrics
    {
        public int UserId { get; set; }
        public int SessionCount { get; set; }
        public TimeSpan TotalSessionTime { get; set; }
        public int FeatureUsageCount { get; set; }
        public DateTime LastActivity { get; set; }
        public Dictionary<string, int> FeatureBreakdown { get; set; }
    }

    public class CustomizationPreviewRequest
    {
        public BrandingConfiguration Branding { get; set; }
        public ThemeConfiguration Theme { get; set; }
        public string PreviewType { get; set; } // "dashboard", "mobile", "email"
        public Dictionary<string, object> SampleData { get; set; }
    }

    public class CustomizationImport
    {
        public string SourceFormat { get; set; } // "json", "xml", "csv"
        public string Data { get; set; }
        public bool OverwriteExisting { get; set; }
        public List<string> FieldMappings { get; set; }
    }

    public class CustomizationAnalytics
    {
        public UsageMetrics Usage { get; set; }
        public List<UserEngagementMetrics> UserEngagement { get; set; }
        public Dictionary<string, int> PopularFeatures { get; set; }
        public Dictionary<string, int> ThemeDistribution { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class CustomizationTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public BrandingConfiguration Branding { get; set; }
        public ThemeConfiguration Theme { get; set; }
        public WorkflowConfiguration Workflow { get; set; }
        public List<CustomField> CustomFields { get; set; }
        public bool IsBuiltIn { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CustomizationPreview
    {
        public Dictionary<string, object> BrandingPreview { get; set; }
        public Dictionary<string, object> ThemePreview { get; set; }
        public Dictionary<string, object> WorkflowPreview { get; set; }
        public string PreviewType { get; set; }
        public Dictionary<string, object> SampleData { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
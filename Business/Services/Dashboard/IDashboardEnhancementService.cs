using Core.Utilities.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Dashboard
{
    /// <summary>
    /// Enhanced dashboard service for sponsor UI/UX improvements
    /// Provides personalized, interactive dashboard components with real-time data
    /// </summary>
    public interface IDashboardEnhancementService
    {
        Task<IDataResult<PersonalizedDashboard>> GetPersonalizedDashboardAsync(int sponsorId);
        Task<IDataResult<DashboardWidgets>> GetDashboardWidgetsAsync(int sponsorId, string layout = "default");
        Task<IDataResult<InteractiveDashboardData>> GetInteractiveDashboardDataAsync(int sponsorId);
        Task<IDataResult<DashboardNotifications>> GetDashboardNotificationsAsync(int sponsorId);
        Task<IDataResult<DashboardPerformanceInsights>> GetPerformanceInsightsAsync(int sponsorId);
        Task<IResult> SaveDashboardPreferencesAsync(int sponsorId, DashboardPreferences preferences);
        Task<IDataResult<RealTimeDashboardUpdates>> GetRealTimeUpdatesAsync(int sponsorId);
        Task<IDataResult<DashboardExportData>> ExportDashboardDataAsync(int sponsorId, string format = "pdf");
    }

    #region Dashboard Data Models

    public class PersonalizedDashboard
    {
        public SponsorPersonalization Personalization { get; set; }
        public List<DashboardCard> Cards { get; set; }
        public DashboardLayoutConfig Layout { get; set; }
        public List<QuickAction> QuickActions { get; set; }
        public PersonalizedInsights Insights { get; set; }
        public DashboardTheme Theme { get; set; }
        public List<DashboardAlert> Alerts { get; set; }
    }

    public class SponsorPersonalization
    {
        public int SponsorId { get; set; }
        public string WelcomeMessage { get; set; }
        public string CompanyName { get; set; }
        public string LogoUrl { get; set; }
        public string PreferredLanguage { get; set; } = "tr";
        public string TimeZone { get; set; } = "Europe/Istanbul";
        public string BusinessGoals { get; set; }
        public List<string> InterestedMetrics { get; set; } = new();
        public DashboardPersonalizationLevel Level { get; set; }
    }

    public enum DashboardPersonalizationLevel
    {
        Basic,
        Intermediate,
        Advanced,
        Expert
    }

    public class DashboardCard
    {
        public string CardId { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public object Value { get; set; }
        public string DisplayValue { get; set; }
        public string Subtitle { get; set; }
        public CardTrend Trend { get; set; }
        public List<CardAction> Actions { get; set; } = new();
        public CardSize Size { get; set; } = CardSize.Medium;
        public bool IsInteractive { get; set; }
        public string DrillDownUrl { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class CardTrend
    {
        public string Direction { get; set; } // up, down, stable
        public double Percentage { get; set; }
        public string Period { get; set; } // vs last week, month, etc.
        public string Color { get; set; }
        public string Description { get; set; }
    }

    public class CardAction
    {
        public string ActionId { get; set; }
        public string Label { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
        public string Color { get; set; }
        public bool RequiresConfirmation { get; set; }
    }

    public enum CardSize
    {
        Small,
        Medium,
        Large,
        ExtraLarge
    }

    public class DashboardLayoutConfig
    {
        public string LayoutType { get; set; } = "responsive"; // grid, masonry, responsive
        public int Columns { get; set; } = 3;
        public List<WidgetPosition> WidgetPositions { get; set; } = new();
        public bool IsDraggable { get; set; } = true;
        public bool IsResizable { get; set; } = true;
        public string BreakpointConfig { get; set; }
    }

    public class WidgetPosition
    {
        public string WidgetId { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;
        public bool IsVisible { get; set; } = true;
    }

    public class QuickAction
    {
        public string ActionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }
        public int UsageCount { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<string> RequiredPermissions { get; set; } = new();
    }

    public class PersonalizedInsights
    {
        public List<Insight> TrendingInsights { get; set; } = new();
        public List<Insight> PerformanceInsights { get; set; } = new();
        public List<Insight> OptimizationSuggestions { get; set; } = new();
        public List<Insight> CompetitiveInsights { get; set; } = new();
        public InsightSummary Summary { get; set; }
    }

    public class Insight
    {
        public string InsightId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; } // trend, performance, optimization, competitive
        public string Severity { get; set; } // low, medium, high, critical
        public string Icon { get; set; }
        public string Color { get; set; }
        public List<InsightAction> Actions { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public bool IsRead { get; set; }
        public double ImpactScore { get; set; }
        public string Category { get; set; }
    }

    public class InsightAction
    {
        public string ActionId { get; set; }
        public string Label { get; set; }
        public string Url { get; set; }
        public string Type { get; set; } // primary, secondary, info
    }

    public class InsightSummary
    {
        public int TotalInsights { get; set; }
        public int UnreadInsights { get; set; }
        public int CriticalInsights { get; set; }
        public int OptimizationOpportunities { get; set; }
        public double OverallHealthScore { get; set; }
    }

    public class DashboardTheme
    {
        public string ThemeId { get; set; } = "default";
        public string PrimaryColor { get; set; } = "#007bff";
        public string SecondaryColor { get; set; } = "#6c757d";
        public string AccentColor { get; set; } = "#28a745";
        public string BackgroundColor { get; set; } = "#f8f9fa";
        public string CardBackgroundColor { get; set; } = "#ffffff";
        public string TextColor { get; set; } = "#212529";
        public string FontFamily { get; set; } = "Inter, sans-serif";
        public bool DarkMode { get; set; }
        public string BorderRadius { get; set; } = "8px";
        public Dictionary<string, string> CustomProperties { get; set; } = new();
    }

    public class DashboardAlert
    {
        public string AlertId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // success, info, warning, error
        public string Icon { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsDismissible { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public List<AlertAction> Actions { get; set; } = new();
    }

    public class AlertAction
    {
        public string ActionId { get; set; }
        public string Label { get; set; }
        public string Url { get; set; }
        public string Style { get; set; } // primary, secondary, link
    }

    public class DashboardWidgets
    {
        public List<Widget> AvailableWidgets { get; set; } = new();
        public List<Widget> ActiveWidgets { get; set; } = new();
        public WidgetConfiguration Configuration { get; set; }
        public List<WidgetCategory> Categories { get; set; } = new();
    }

    public class Widget
    {
        public string WidgetId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Category { get; set; }
        public WidgetSize Size { get; set; }
        public object Data { get; set; }
        public WidgetSettings Settings { get; set; }
        public bool IsConfigurable { get; set; }
        public bool IsRemovable { get; set; } = true;
        public List<string> Dependencies { get; set; } = new();
        public WidgetRefreshConfig RefreshConfig { get; set; }
    }

    public class WidgetSize
    {
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;
        public int MinWidth { get; set; } = 1;
        public int MinHeight { get; set; } = 1;
        public int MaxWidth { get; set; } = 4;
        public int MaxHeight { get; set; } = 4;
    }

    public class WidgetSettings
    {
        public string Title { get; set; }
        public bool ShowHeader { get; set; } = true;
        public bool ShowFooter { get; set; }
        public string RefreshInterval { get; set; } = "5m";
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    public class WidgetRefreshConfig
    {
        public bool AutoRefresh { get; set; }
        public int IntervalSeconds { get; set; } = 300; // 5 minutes
        public List<string> RefreshTriggers { get; set; } = new();
        public DateTime LastRefresh { get; set; }
    }

    public class WidgetConfiguration
    {
        public string Layout { get; set; } = "responsive";
        public bool AllowCustomization { get; set; } = true;
        public bool AllowWidgetAddition { get; set; } = true;
        public bool AllowWidgetRemoval { get; set; } = true;
        public List<string> AvailableLayouts { get; set; } = new();
    }

    public class WidgetCategory
    {
        public string CategoryId { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public int WidgetCount { get; set; }
        public bool IsExpanded { get; set; } = true;
    }

    public class InteractiveDashboardData
    {
        public List<InteractiveChart> Charts { get; set; } = new();
        public List<InteractiveTable> Tables { get; set; } = new();
        public List<FilterOption> FilterOptions { get; set; } = new();
        public DrillDownConfig DrillDownConfig { get; set; }
        public List<DashboardAction> ContextualActions { get; set; } = new();
    }

    public class InteractiveChart
    {
        public string ChartId { get; set; }
        public string Title { get; set; }
        public string ChartType { get; set; } // line, bar, pie, donut, area, scatter
        public object Data { get; set; }
        public ChartOptions Options { get; set; }
        public bool IsInteractive { get; set; } = true;
        public List<ChartFilter> Filters { get; set; } = new();
        public bool AllowExport { get; set; } = true;
    }

    public class ChartOptions
    {
        public bool ShowLegend { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public bool ShowTooltips { get; set; } = true;
        public bool AllowZoom { get; set; } = true;
        public string Theme { get; set; } = "default";
        public Dictionary<string, object> CustomOptions { get; set; } = new();
    }

    public class ChartFilter
    {
        public string FilterId { get; set; }
        public string Label { get; set; }
        public string Type { get; set; } // dropdown, daterange, multiselect
        public List<FilterValue> Values { get; set; } = new();
        public object SelectedValue { get; set; }
    }

    public class FilterValue
    {
        public string Value { get; set; }
        public string Label { get; set; }
        public bool IsSelected { get; set; }
    }

    public class InteractiveTable
    {
        public string TableId { get; set; }
        public string Title { get; set; }
        public List<TableColumn> Columns { get; set; } = new();
        public List<Dictionary<string, object>> Rows { get; set; } = new();
        public TablePagination Pagination { get; set; }
        public bool AllowSorting { get; set; } = true;
        public bool AllowFiltering { get; set; } = true;
        public bool AllowExport { get; set; } = true;
    }

    public class TableColumn
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Type { get; set; } = "text";
        public bool Sortable { get; set; } = true;
        public bool Filterable { get; set; } = true;
        public string Width { get; set; }
        public string Alignment { get; set; } = "left";
    }

    public class TablePagination
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRows { get; set; }
        public int TotalPages { get; set; }
        public bool ShowPageSize { get; set; } = true;
    }

    public class FilterOption
    {
        public string FilterId { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        public List<FilterValue> Options { get; set; } = new();
        public object DefaultValue { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    public class DrillDownConfig
    {
        public bool Enabled { get; set; } = true;
        public List<DrillDownPath> Paths { get; set; } = new();
        public string DefaultPath { get; set; }
    }

    public class DrillDownPath
    {
        public string PathId { get; set; }
        public string Label { get; set; }
        public string Url { get; set; }
        public List<string> RequiredParams { get; set; } = new();
    }

    public class DashboardAction
    {
        public string ActionId { get; set; }
        public string Label { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
        public string Method { get; set; } = "GET";
        public string ConfirmationMessage { get; set; }
        public bool RequiresConfirmation { get; set; }
        public List<string> RequiredPermissions { get; set; } = new();
    }

    public class DashboardNotifications
    {
        public List<Notification> Notifications { get; set; } = new();
        public NotificationSummary Summary { get; set; }
        public NotificationSettings Settings { get; set; }
    }

    public class Notification
    {
        public string NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // info, success, warning, error
        public string Icon { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string Priority { get; set; } // low, medium, high, urgent
        public string Category { get; set; }
        public List<NotificationAction> Actions { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class NotificationAction
    {
        public string ActionId { get; set; }
        public string Label { get; set; }
        public string Url { get; set; }
        public string Method { get; set; } = "GET";
    }

    public class NotificationSummary
    {
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int HighPriorityNotifications { get; set; }
        public int UrgentNotifications { get; set; }
        public DateTime? LastNotificationDate { get; set; }
    }

    public class NotificationSettings
    {
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool InAppNotifications { get; set; } = true;
        public List<string> EnabledCategories { get; set; } = new();
        public string NotificationFrequency { get; set; } = "immediate";
    }

    public class DashboardPerformanceInsights
    {
        public PerformanceOverview Overview { get; set; }
        public List<PerformanceMetric> KeyMetrics { get; set; } = new();
        public List<PerformanceTrend> Trends { get; set; } = new();
        public List<PerformanceRecommendation> Recommendations { get; set; } = new();
        public BenchmarkComparison Benchmark { get; set; }
    }

    public class PerformanceOverview
    {
        public double OverallScore { get; set; }
        public string ScoreGrade { get; set; } // A, B, C, D, F
        public string PerformanceLevel { get; set; } // Excellent, Good, Average, Poor
        public List<string> TopAchievements { get; set; } = new();
        public List<string> AreasForImprovement { get; set; } = new();
    }

    public class PerformanceMetric
    {
        public string MetricId { get; set; }
        public string Name { get; set; }
        public object CurrentValue { get; set; }
        public object TargetValue { get; set; }
        public string Unit { get; set; }
        public double ProgressPercentage { get; set; }
        public string Status { get; set; } // on_track, behind, ahead
        public PerformanceTrend Trend { get; set; }
    }

    public class PerformanceTrend
    {
        public string TrendId { get; set; }
        public string Name { get; set; }
        public string Direction { get; set; } // up, down, stable
        public double ChangePercentage { get; set; }
        public string Period { get; set; }
        public List<TrendDataPoint> DataPoints { get; set; } = new();
    }

    public class TrendDataPoint
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public string Label { get; set; }
    }

    public class PerformanceRecommendation
    {
        public string RecommendationId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; } // low, medium, high, critical
        public string Category { get; set; }
        public double ExpectedImpact { get; set; }
        public string EstimatedTimeToImplement { get; set; }
        public List<RecommendationStep> Steps { get; set; } = new();
    }

    public class RecommendationStep
    {
        public int StepNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class BenchmarkComparison
    {
        public string Industry { get; set; }
        public List<BenchmarkMetric> Metrics { get; set; } = new();
        public string Percentile { get; set; }
        public string CompetitivePosition { get; set; } // leading, competitive, trailing
    }

    public class BenchmarkMetric
    {
        public string MetricName { get; set; }
        public double YourValue { get; set; }
        public double IndustryAverage { get; set; }
        public double TopPerformerValue { get; set; }
        public string ComparisonStatus { get; set; } // above_average, average, below_average
    }

    public class DashboardPreferences
    {
        public string Layout { get; set; } = "responsive";
        public string Theme { get; set; } = "default";
        public bool DarkMode { get; set; }
        public string Language { get; set; } = "tr";
        public string TimeZone { get; set; } = "Europe/Istanbul";
        public List<string> EnabledWidgets { get; set; } = new();
        public List<WidgetPosition> WidgetPositions { get; set; } = new();
        public NotificationSettings NotificationSettings { get; set; }
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    public class RealTimeDashboardUpdates
    {
        public List<RealTimeUpdate> Updates { get; set; } = new();
        public DateTime LastUpdateTime { get; set; }
        public string NextUpdateIn { get; set; }
        public bool IsConnected { get; set; } = true;
        public string ConnectionStatus { get; set; } = "connected";
    }

    public class RealTimeUpdate
    {
        public string UpdateId { get; set; }
        public string Type { get; set; } // metric_update, notification, alert
        public string Target { get; set; } // widget_id, card_id, etc.
        public object Data { get; set; }
        public DateTime Timestamp { get; set; }
        public string Priority { get; set; } = "normal";
    }

    public class DashboardExportData
    {
        public string ExportId { get; set; }
        public string Format { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public long FileSizeBytes { get; set; }
        public string Status { get; set; } = "ready";
        public ExportMetadata Metadata { get; set; }
    }

    public class ExportMetadata
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DateRange { get; set; }
        public List<string> IncludedSections { get; set; } = new();
        public string Template { get; set; } = "standard";
    }

    #endregion
}
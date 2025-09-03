using Core.Utilities.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Customization
{
    /// <summary>
    /// Advanced customization service for sponsor-specific branding, themes, and feature configurations
    /// Provides white-label solutions, custom workflows, and enterprise-grade personalization
    /// </summary>
    public interface ICustomizationService
    {
        Task<IDataResult<SponsorCustomization>> GetSponsorCustomizationAsync(int sponsorId);
        Task<IResult> UpdateSponsorCustomizationAsync(int sponsorId, SponsorCustomizationUpdate update);
        Task<IDataResult<BrandingConfiguration>> GetBrandingConfigurationAsync(int sponsorId);
        Task<IResult> UpdateBrandingConfigurationAsync(int sponsorId, BrandingConfiguration branding);
        Task<IDataResult<ThemeConfiguration>> GetThemeConfigurationAsync(int sponsorId);
        Task<IResult> UpdateThemeConfigurationAsync(int sponsorId, ThemeConfiguration theme);
        Task<IDataResult<WorkflowConfiguration>> GetWorkflowConfigurationAsync(int sponsorId);
        Task<IResult> UpdateWorkflowConfigurationAsync(int sponsorId, WorkflowConfiguration workflow);
        Task<IDataResult<List<CustomField>>> GetCustomFieldsAsync(int sponsorId, string category = "all");
        Task<IResult> AddCustomFieldAsync(int sponsorId, CustomField field);
        Task<IResult> UpdateCustomFieldAsync(int sponsorId, string fieldId, CustomField field);
        Task<IResult> DeleteCustomFieldAsync(int sponsorId, string fieldId);
        Task<IDataResult<List<CustomizationTemplate>>> GetCustomizationTemplatesAsync(string category = "all");
        Task<IResult> ApplyCustomizationTemplateAsync(int sponsorId, string templateId);
        Task<IDataResult<WhiteLabelConfiguration>> GetWhiteLabelConfigurationAsync(int sponsorId);
        Task<IResult> UpdateWhiteLabelConfigurationAsync(int sponsorId, WhiteLabelConfiguration whiteLabel);
        Task<IDataResult<CustomizationPreview>> PreviewCustomizationAsync(int sponsorId, CustomizationPreviewRequest request);
        Task<IResult> ExportCustomizationAsync(int sponsorId, string format = "json");
        Task<IResult> ImportCustomizationAsync(int sponsorId, CustomizationImport import);
        Task<IDataResult<CustomizationAnalytics>> GetCustomizationAnalyticsAsync(int sponsorId);
    }

    #region Customization Data Models

    public class SponsorCustomization
    {
        public int SponsorId { get; set; }
        public string CustomizationLevel { get; set; } = "basic"; // basic, advanced, enterprise, white_label
        public BrandingConfiguration Branding { get; set; } = new();
        public ThemeConfiguration Theme { get; set; } = new();
        public WorkflowConfiguration Workflow { get; set; } = new();
        public List<CustomField> CustomFields { get; set; } = new();
        public WhiteLabelConfiguration WhiteLabel { get; set; } = new();
        public CustomizationSettings Settings { get; set; } = new();
        public CustomizationMetadata Metadata { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SponsorCustomizationUpdate
    {
        public string CustomizationLevel { get; set; }
        public BrandingConfiguration Branding { get; set; }
        public ThemeConfiguration Theme { get; set; }
        public WorkflowConfiguration Workflow { get; set; }
        public WhiteLabelConfiguration WhiteLabel { get; set; }
        public CustomizationSettings Settings { get; set; }
    }

    public class BrandingConfiguration
    {
        public CompanyBranding Company { get; set; } = new();
        public LogoConfiguration Logo { get; set; } = new();
        public ColorPalette Colors { get; set; } = new();
        public Typography Typography { get; set; } = new();
        public ImageAssets Images { get; set; } = new();
        public ContactInformation Contact { get; set; } = new();
        public BrandingAssets Assets { get; set; } = new();
        public string BrandGuidelines { get; set; }
        public Dictionary<string, string> CustomBrandElements { get; set; } = new();
    }

    public class CompanyBranding
    {
        public string CompanyName { get; set; }
        public string DisplayName { get; set; }
        public string Tagline { get; set; }
        public string Description { get; set; }
        public string Industry { get; set; }
        public string CompanySize { get; set; }
        public string FoundedYear { get; set; }
        public string Mission { get; set; }
        public string Vision { get; set; }
        public List<string> Values { get; set; } = new();
        public string CompanyType { get; set; } // corporation, cooperative, ngo, startup
    }

    public class LogoConfiguration
    {
        public string PrimaryLogoUrl { get; set; }
        public string SecondaryLogoUrl { get; set; }
        public string FaviconUrl { get; set; }
        public string WatermarkUrl { get; set; }
        public LogoVariants Variants { get; set; } = new();
        public LogoSpecs Specifications { get; set; } = new();
        public string LogoPlacement { get; set; } = "top_left"; // top_left, top_center, top_right, custom
    }

    public class LogoVariants
    {
        public string DarkModeUrl { get; set; }
        public string LightModeUrl { get; set; }
        public string MinimalUrl { get; set; }
        public string IconOnlyUrl { get; set; }
        public string SquareUrl { get; set; }
        public string HorizontalUrl { get; set; }
        public string VerticalUrl { get; set; }
    }

    public class LogoSpecs
    {
        public string PreferredFormat { get; set; } = "svg"; // svg, png, jpg
        public string MinimumSize { get; set; } = "32x32";
        public string MaximumSize { get; set; } = "512x512";
        public string BackgroundColor { get; set; } = "transparent";
        public bool AllowBackgroundRemoval { get; set; } = true;
    }

    public class ColorPalette
    {
        public string Primary { get; set; } = "#007bff";
        public string Secondary { get; set; } = "#6c757d";
        public string Success { get; set; } = "#28a745";
        public string Warning { get; set; } = "#ffc107";
        public string Danger { get; set; } = "#dc3545";
        public string Info { get; set; } = "#17a2b8";
        public string Light { get; set; } = "#f8f9fa";
        public string Dark { get; set; } = "#343a40";
        public string Background { get; set; } = "#ffffff";
        public string Text { get; set; } = "#212529";
        public string Accent { get; set; } = "#e9ecef";
        public Dictionary<string, string> CustomColors { get; set; } = new();
        public ColorAccessibility Accessibility { get; set; } = new();
    }

    public class ColorAccessibility
    {
        public bool HighContrast { get; set; }
        public bool ColorBlindFriendly { get; set; }
        public double MinimumContrastRatio { get; set; } = 4.5;
        public List<string> AlternativeColorSchemes { get; set; } = new();
    }

    public class Typography
    {
        public FontConfiguration Primary { get; set; } = new();
        public FontConfiguration Secondary { get; set; } = new();
        public FontConfiguration Headings { get; set; } = new();
        public FontConfiguration Body { get; set; } = new();
        public FontConfiguration Monospace { get; set; } = new();
        public TypographyScale Scale { get; set; } = new();
        public TypographySettings Settings { get; set; } = new();
    }

    public class FontConfiguration
    {
        public string FontFamily { get; set; } = "Inter, sans-serif";
        public string FontWeight { get; set; } = "400";
        public string FontSize { get; set; } = "16px";
        public string LineHeight { get; set; } = "1.5";
        public string LetterSpacing { get; set; } = "normal";
        public string FontSource { get; set; } = "system"; // system, google_fonts, custom
        public string CustomFontUrl { get; set; }
    }

    public class TypographyScale
    {
        public string H1 { get; set; } = "2.5rem";
        public string H2 { get; set; } = "2rem";
        public string H3 { get; set; } = "1.75rem";
        public string H4 { get; set; } = "1.5rem";
        public string H5 { get; set; } = "1.25rem";
        public string H6 { get; set; } = "1rem";
        public string Body { get; set; } = "1rem";
        public string Small { get; set; } = "0.875rem";
        public string Caption { get; set; } = "0.75rem";
    }

    public class TypographySettings
    {
        public bool UseSystemFonts { get; set; } = true;
        public bool AllowUserFontSizeAdjustment { get; set; } = true;
        public string DefaultTextDirection { get; set; } = "ltr"; // ltr, rtl
        public double ReadabilityOptimization { get; set; } = 1.0;
    }

    public class ImageAssets
    {
        public string BackgroundImage { get; set; }
        public string HeroImage { get; set; }
        public string PatternImage { get; set; }
        public string PlaceholderImage { get; set; }
        public List<string> GalleryImages { get; set; } = new();
        public IconSet Icons { get; set; } = new();
        public ImageSettings Settings { get; set; } = new();
    }

    public class IconSet
    {
        public string IconLibrary { get; set; } = "font_awesome"; // font_awesome, material, custom
        public string CustomIconUrl { get; set; }
        public Dictionary<string, string> CustomIcons { get; set; } = new();
        public string IconStyle { get; set; } = "filled"; // filled, outlined, rounded
    }

    public class ImageSettings
    {
        public string DefaultFormat { get; set; } = "webp";
        public bool LazyLoading { get; set; } = true;
        public bool OptimizeForMobile { get; set; } = true;
        public string CompressionLevel { get; set; } = "medium";
    }

    public class ContactInformation
    {
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
        public Address Address { get; set; } = new();
        public SocialMedia SocialMedia { get; set; } = new();
        public BusinessHours BusinessHours { get; set; } = new();
        public SupportInformation Support { get; set; } = new();
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string FormattedAddress { get; set; }
        public GeoLocation GeoLocation { get; set; }
    }

    public class GeoLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string GoogleMapsUrl { get; set; }
    }

    public class SocialMedia
    {
        public string Facebook { get; set; }
        public string Twitter { get; set; }
        public string LinkedIn { get; set; }
        public string Instagram { get; set; }
        public string YouTube { get; set; }
        public Dictionary<string, string> CustomPlatforms { get; set; } = new();
    }

    public class BusinessHours
    {
        public Dictionary<string, DaySchedule> WeeklySchedule { get; set; } = new();
        public string TimeZone { get; set; } = "Europe/Istanbul";
        public List<string> Holidays { get; set; } = new();
        public string SpecialNotice { get; set; }
    }

    public class DaySchedule
    {
        public bool IsOpen { get; set; } = true;
        public string OpenTime { get; set; } = "09:00";
        public string CloseTime { get; set; } = "17:00";
        public string BreakTime { get; set; }
        public string Notes { get; set; }
    }

    public class SupportInformation
    {
        public string SupportEmail { get; set; }
        public string SupportPhone { get; set; }
        public string SupportUrl { get; set; }
        public string KnowledgeBaseUrl { get; set; }
        public string LiveChatUrl { get; set; }
        public SupportHours SupportHours { get; set; } = new();
    }

    public class SupportHours
    {
        public string AvailabilityHours { get; set; } = "24/7";
        public string ResponseTime { get; set; } = "24 hours";
        public List<string> SupportLanguages { get; set; } = new();
        public string EscalationProcess { get; set; }
    }

    public class BrandingAssets
    {
        public List<BrandAsset> Documents { get; set; } = new();
        public List<BrandAsset> Templates { get; set; } = new();
        public List<BrandAsset> MediaKit { get; set; } = new();
        public BrandGuidelines Guidelines { get; set; } = new();
    }

    public class BrandAsset
    {
        public string AssetId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string Type { get; set; } // image, document, video, template
        public string Category { get; set; }
        public long SizeBytes { get; set; }
        public string Format { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BrandGuidelines
    {
        public string LogoUsage { get; set; }
        public string ColorUsage { get; set; }
        public string TypographyUsage { get; set; }
        public string ImageryGuidelines { get; set; }
        public string ToneOfVoice { get; set; }
        public string DoAndDonts { get; set; }
        public string BrandPersonality { get; set; }
    }

    public class ThemeConfiguration
    {
        public string ThemeId { get; set; } = "default";
        public string ThemeName { get; set; } = "Default Theme";
        public string ThemeType { get; set; } = "light"; // light, dark, auto, custom
        public LayoutConfiguration Layout { get; set; } = new();
        public ComponentStyling Components { get; set; } = new();
        public AnimationSettings Animations { get; set; } = new();
        public ResponsiveSettings Responsive { get; set; } = new();
        public AccessibilitySettings Accessibility { get; set; } = new();
        public ThemeCustomization Customization { get; set; } = new();
        public string CssOverrides { get; set; }
        public string PreprocessorVariables { get; set; }
    }

    public class LayoutConfiguration
    {
        public string LayoutType { get; set; } = "fluid"; // fluid, fixed, boxed
        public string HeaderStyle { get; set; } = "default"; // default, minimal, full_width
        public string SidebarStyle { get; set; } = "default"; // default, minimal, collapsed
        public string FooterStyle { get; set; } = "default"; // default, minimal, sticky
        public bool ShowBreadcrumbs { get; set; } = true;
        public string NavigationStyle { get; set; } = "horizontal"; // horizontal, vertical, tabs
        public LayoutSpacing Spacing { get; set; } = new();
        public LayoutGrid Grid { get; set; } = new();
    }

    public class LayoutSpacing
    {
        public string ContentPadding { get; set; } = "1rem";
        public string SectionMargin { get; set; } = "2rem";
        public string ComponentSpacing { get; set; } = "1rem";
        public string VerticalRhythm { get; set; } = "1.5rem";
    }

    public class LayoutGrid
    {
        public int MaxColumns { get; set; } = 12;
        public string GutterSize { get; set; } = "1rem";
        public string MaxWidth { get; set; } = "1200px";
        public string Breakpoints { get; set; } = "xs: 0, sm: 576px, md: 768px, lg: 992px, xl: 1200px";
    }

    public class ComponentStyling
    {
        public ButtonStyling Buttons { get; set; } = new();
        public FormStyling Forms { get; set; } = new();
        public CardStyling Cards { get; set; } = new();
        public NavigationStyling Navigation { get; set; } = new();
        public TableStyling Tables { get; set; } = new();
        public Dictionary<string, object> CustomComponents { get; set; } = new();
    }

    public class ButtonStyling
    {
        public string BorderRadius { get; set; } = "4px";
        public string FontWeight { get; set; } = "500";
        public string Padding { get; set; } = "0.5rem 1rem";
        public string Transition { get; set; } = "all 0.3s ease";
        public string HoverEffect { get; set; } = "darken";
        public bool DropShadow { get; set; } = true;
    }

    public class FormStyling
    {
        public string InputBorderRadius { get; set; } = "4px";
        public string InputPadding { get; set; } = "0.75rem";
        public string LabelStyle { get; set; } = "floating"; // floating, static, inline
        public string ValidationStyle { get; set; } = "inline"; // inline, tooltip, below
        public bool ShowRequiredIndicator { get; set; } = true;
    }

    public class CardStyling
    {
        public string BorderRadius { get; set; } = "8px";
        public string BoxShadow { get; set; } = "0 2px 4px rgba(0,0,0,0.1)";
        public string Padding { get; set; } = "1.5rem";
        public string BackgroundColor { get; set; } = "#ffffff";
        public string BorderColor { get; set; } = "#e9ecef";
    }

    public class NavigationStyling
    {
        public string ActiveIndicator { get; set; } = "underline"; // underline, background, border
        public string HoverEffect { get; set; } = "background"; // background, underline, scale
        public string MenuStyle { get; set; } = "dropdown"; // dropdown, mega, sidebar
        public bool ShowIcons { get; set; } = true;
        public bool ShowLabels { get; set; } = true;
    }

    public class TableStyling
    {
        public string RowHoverColor { get; set; } = "#f8f9fa";
        public string BorderStyle { get; set; } = "horizontal"; // all, horizontal, none
        public string HeaderStyle { get; set; } = "default"; // default, minimal, bold
        public bool ShowPagination { get; set; } = true;
        public bool ShowSearch { get; set; } = true;
    }

    public class AnimationSettings
    {
        public bool EnableAnimations { get; set; } = true;
        public string AnimationDuration { get; set; } = "300ms";
        public string AnimationEasing { get; set; } = "ease-in-out";
        public TransitionEffects Transitions { get; set; } = new();
        public LoadingAnimations Loading { get; set; } = new();
        public bool ReducedMotion { get; set; } = false;
    }

    public class TransitionEffects
    {
        public string PageTransition { get; set; } = "fade"; // fade, slide, scale, none
        public string ModalTransition { get; set; } = "fade_scale";
        public string TooltipTransition { get; set; } = "fade";
        public string DropdownTransition { get; set; } = "slide_down";
    }

    public class LoadingAnimations
    {
        public string LoadingSpinner { get; set; } = "default"; // default, dots, bars, pulse
        public string SkeletonStyle { get; set; } = "wave"; // wave, pulse, shimmer
        public string ProgressBarStyle { get; set; } = "smooth"; // smooth, stepped, pulse
    }

    public class ResponsiveSettings
    {
        public bool MobileFirst { get; set; } = true;
        public Dictionary<string, BreakpointSettings> Breakpoints { get; set; } = new();
        public MobileSettings Mobile { get; set; } = new();
        public TabletSettings Tablet { get; set; } = new();
        public DesktopSettings Desktop { get; set; } = new();
    }

    public class BreakpointSettings
    {
        public string Name { get; set; }
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }
        public string LayoutOverrides { get; set; }
        public string TypographyOverrides { get; set; }
        public string SpacingOverrides { get; set; }
    }

    public class MobileSettings
    {
        public bool TouchOptimized { get; set; } = true;
        public string MenuStyle { get; set; } = "hamburger"; // hamburger, tabs, bottom_nav
        public bool SwipeGestures { get; set; } = true;
        public string OrientationHandling { get; set; } = "adaptive"; // adaptive, fixed, responsive
    }

    public class TabletSettings
    {
        public string LayoutMode { get; set; } = "hybrid"; // mobile, desktop, hybrid
        public bool AdaptiveNavigation { get; set; } = true;
        public string InteractionMode { get; set; } = "touch_first"; // touch_first, mouse_first, hybrid
    }

    public class DesktopSettings
    {
        public string DefaultSidebarState { get; set; } = "expanded"; // expanded, collapsed, auto
        public bool KeyboardShortcuts { get; set; } = true;
        public string WindowMode { get; set; } = "fullscreen"; // fullscreen, windowed, adaptive
    }

    public class AccessibilitySettings
    {
        public bool HighContrast { get; set; } = false;
        public bool ReducedMotion { get; set; } = false;
        public bool ScreenReaderOptimized { get; set; } = true;
        public string FocusIndicatorStyle { get; set; } = "outline"; // outline, background, border
        public bool KeyboardNavigation { get; set; } = true;
        public string FontSizeMultiplier { get; set; } = "1.0";
        public ColorBlindSupport ColorBlindSupport { get; set; } = new();
        public SemanticMarkup SemanticMarkup { get; set; } = new();
    }

    public class ColorBlindSupport
    {
        public bool AlternativeColorScheme { get; set; } = false;
        public bool PatternIndicators { get; set; } = false;
        public string ColorBlindType { get; set; } = "none"; // none, protanopia, deuteranopia, tritanopia
    }

    public class SemanticMarkup
    {
        public bool UseSemanticHtml { get; set; } = true;
        public bool AriaLabels { get; set; } = true;
        public bool LandmarkRoles { get; set; } = true;
        public bool HeadingHierarchy { get; set; } = true;
    }

    public class ThemeCustomization
    {
        public Dictionary<string, string> CssVariables { get; set; } = new();
        public Dictionary<string, object> ComponentOverrides { get; set; } = new();
        public string CustomCss { get; set; }
        public string CustomJavascript { get; set; }
        public List<string> CustomFonts { get; set; } = new();
        public List<string> ExternalStylesheets { get; set; } = new();
    }

    public class WorkflowConfiguration
    {
        public Dictionary<string, WorkflowStep> CustomWorkflows { get; set; } = new();
        public NotificationConfiguration Notifications { get; set; } = new();
        public ApprovalConfiguration Approvals { get; set; } = new();
        public AutomationConfiguration Automation { get; set; } = new();
        public IntegrationConfiguration Integrations { get; set; } = new();
        public CustomBusinessRules BusinessRules { get; set; } = new();
    }

    public class WorkflowStep
    {
        public string StepId { get; set; }
        public string StepName { get; set; }
        public string StepType { get; set; } // approval, notification, automation, integration
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<string> NextSteps { get; set; } = new();
        public List<WorkflowCondition> Conditions { get; set; } = new();
        public bool IsRequired { get; set; } = true;
        public int TimeoutMinutes { get; set; } = 60;
    }

    public class WorkflowCondition
    {
        public string Field { get; set; }
        public string Operator { get; set; } // equals, contains, greater_than, etc.
        public object Value { get; set; }
        public string LogicalOperator { get; set; } = "AND"; // AND, OR
    }

    public class NotificationConfiguration
    {
        public EmailNotifications Email { get; set; } = new();
        public SmsNotifications Sms { get; set; } = new();
        public WhatsAppNotifications WhatsApp { get; set; } = new();
        public InAppNotifications InApp { get; set; } = new();
        public PushNotifications Push { get; set; } = new();
        public WebhookNotifications Webhooks { get; set; } = new();
    }

    public class EmailNotifications
    {
        public bool Enabled { get; set; } = true;
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string ReplyToEmail { get; set; }
        public Dictionary<string, EmailTemplate> Templates { get; set; } = new();
        public EmailSettings Settings { get; set; } = new();
    }

    public class EmailTemplate
    {
        public string TemplateId { get; set; }
        public string Subject { get; set; }
        public string HtmlContent { get; set; }
        public string TextContent { get; set; }
        public List<string> Variables { get; set; } = new();
        public string Language { get; set; } = "tr";
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public bool UseTls { get; set; } = true;
        public bool UseCustomSender { get; set; } = false;
    }

    public class SmsNotifications
    {
        public bool Enabled { get; set; } = true;
        public string Provider { get; set; } = "turkcell";
        public string SenderId { get; set; }
        public Dictionary<string, string> Templates { get; set; } = new();
        public SmsSettings Settings { get; set; } = new();
    }

    public class SmsSettings
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string ApiUrl { get; set; }
        public bool UseUnicode { get; set; } = true;
        public string DefaultCountryCode { get; set; } = "+90";
    }

    public class WhatsAppNotifications
    {
        public bool Enabled { get; set; } = true;
        public string BusinessApiKey { get; set; }
        public string PhoneNumberId { get; set; }
        public Dictionary<string, WhatsAppTemplate> Templates { get; set; } = new();
        public WhatsAppSettings Settings { get; set; } = new();
    }

    public class WhatsAppTemplate
    {
        public string TemplateName { get; set; }
        public string Language { get; set; }
        public List<TemplateComponent> Components { get; set; } = new();
        public string Category { get; set; }
        public string Status { get; set; }
    }

    public class TemplateComponent
    {
        public string Type { get; set; } // header, body, footer, button
        public List<ComponentParameter> Parameters { get; set; } = new();
        public string Text { get; set; }
    }

    public class ComponentParameter
    {
        public string Type { get; set; } // text, currency, datetime, image, video
        public string Text { get; set; }
        public object Value { get; set; }
    }

    public class WhatsAppSettings
    {
        public string AccessToken { get; set; }
        public string WebhookVerifyToken { get; set; }
        public string WebhookUrl { get; set; }
        public bool AutoRetryFailedMessages { get; set; } = true;
        public int RetryAttempts { get; set; } = 3;
    }

    public class InAppNotifications
    {
        public bool Enabled { get; set; } = true;
        public string NotificationStyle { get; set; } = "toast"; // toast, banner, modal
        public string Position { get; set; } = "top_right"; // top_right, top_left, bottom_right, bottom_left
        public int AutoHideSeconds { get; set; } = 5;
        public bool ShowUnreadCount { get; set; } = true;
        public string SoundUrl { get; set; }
    }

    public class PushNotifications
    {
        public bool Enabled { get; set; } = true;
        public string ServiceProvider { get; set; } = "firebase"; // firebase, onesignal, custom
        public string ApiKey { get; set; }
        public string SenderId { get; set; }
        public Dictionary<string, string> Templates { get; set; } = new();
        public PushSettings Settings { get; set; } = new();
    }

    public class PushSettings
    {
        public string DefaultIcon { get; set; }
        public string DefaultSound { get; set; }
        public bool ShowBadge { get; set; } = true;
        public bool VibrationEnabled { get; set; } = true;
        public string Priority { get; set; } = "high"; // high, normal
    }

    public class WebhookNotifications
    {
        public bool Enabled { get; set; } = false;
        public List<WebhookEndpoint> Endpoints { get; set; } = new();
        public WebhookSettings Settings { get; set; } = new();
    }

    public class WebhookEndpoint
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Method { get; set; } = "POST";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string AuthType { get; set; } = "none"; // none, basic, bearer, custom
        public string AuthValue { get; set; }
        public List<string> Events { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class WebhookSettings
    {
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 60;
        public bool VerifySSL { get; set; } = true;
        public string SignatureSecret { get; set; }
    }

    public class ApprovalConfiguration
    {
        public bool RequireApproval { get; set; } = false;
        public Dictionary<string, ApprovalRule> ApprovalRules { get; set; } = new();
        public List<ApprovalStep> ApprovalSteps { get; set; } = new();
        public ApprovalSettings Settings { get; set; } = new();
    }

    public class ApprovalRule
    {
        public string RuleName { get; set; }
        public List<WorkflowCondition> Conditions { get; set; } = new();
        public List<string> RequiredApprovers { get; set; } = new();
        public int MinimumApprovals { get; set; } = 1;
        public bool AllApprovalsRequired { get; set; } = false;
        public int TimeoutHours { get; set; } = 24;
    }

    public class ApprovalStep
    {
        public string StepName { get; set; }
        public List<string> Approvers { get; set; } = new();
        public int Order { get; set; }
        public bool IsParallel { get; set; } = false;
        public string EscalationRule { get; set; }
    }

    public class ApprovalSettings
    {
        public bool AutoApproveAfterTimeout { get; set; } = false;
        public bool SendReminderNotifications { get; set; } = true;
        public int ReminderIntervalHours { get; set; } = 12;
        public string DefaultApprover { get; set; }
        public bool AllowSelfApproval { get; set; } = false;
    }

    public class AutomationConfiguration
    {
        public bool Enabled { get; set; } = true;
        public List<AutomationRule> Rules { get; set; } = new();
        public ScheduledTasks ScheduledTasks { get; set; } = new();
        public EventTriggers EventTriggers { get; set; } = new();
        public AutomationSettings Settings { get; set; } = new();
    }

    public class AutomationRule
    {
        public string RuleId { get; set; }
        public string RuleName { get; set; }
        public string TriggerEvent { get; set; }
        public List<WorkflowCondition> Conditions { get; set; } = new();
        public List<AutomationAction> Actions { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public string Priority { get; set; } = "normal"; // high, normal, low
    }

    public class AutomationAction
    {
        public string ActionType { get; set; } // send_notification, update_status, create_record, call_webhook
        public Dictionary<string, object> Parameters { get; set; } = new();
        public int DelaySeconds { get; set; } = 0;
        public bool ContinueOnError { get; set; } = true;
    }

    public class ScheduledTasks
    {
        public List<ScheduledTask> Tasks { get; set; } = new();
        public TaskSchedulerSettings Settings { get; set; } = new();
    }

    public class ScheduledTask
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string Schedule { get; set; } // cron expression
        public List<AutomationAction> Actions { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public string TimeZone { get; set; } = "Europe/Istanbul";
    }

    public class TaskSchedulerSettings
    {
        public int MaxConcurrentTasks { get; set; } = 5;
        public int TaskTimeoutMinutes { get; set; } = 30;
        public bool LogTaskExecution { get; set; } = true;
        public string FailureNotificationEmail { get; set; }
    }

    public class EventTriggers
    {
        public List<EventTrigger> Triggers { get; set; } = new();
        public EventSettings Settings { get; set; } = new();
    }

    public class EventTrigger
    {
        public string EventName { get; set; }
        public string EventType { get; set; } // entity_created, entity_updated, custom_event
        public List<WorkflowCondition> Conditions { get; set; } = new();
        public List<AutomationAction> Actions { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class EventSettings
    {
        public bool EnableEventLogging { get; set; } = true;
        public int EventRetentionDays { get; set; } = 30;
        public int MaxEventsPerMinute { get; set; } = 100;
        public bool ThrottleEvents { get; set; } = true;
    }

    public class AutomationSettings
    {
        public bool EnableDebugMode { get; set; } = false;
        public int MaxRuleExecutionTime { get; set; } = 300; // seconds
        public bool AllowRecursiveRules { get; set; } = false;
        public string ErrorHandling { get; set; } = "continue"; // stop, continue, retry
    }

    public class IntegrationConfiguration
    {
        public ApiIntegrations Api { get; set; } = new();
        public ThirdPartyIntegrations ThirdParty { get; set; } = new();
        public DatabaseIntegrations Database { get; set; } = new();
        public FileIntegrations Files { get; set; } = new();
        public IntegrationSettings Settings { get; set; } = new();
    }

    public class ApiIntegrations
    {
        public List<ApiEndpoint> CustomEndpoints { get; set; } = new();
        public AuthenticationMethods Authentication { get; set; } = new();
        public ApiSettings Settings { get; set; } = new();
    }

    public class ApiEndpoint
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Method { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public string RequestBody { get; set; }
        public string ResponseMapping { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AuthenticationMethods
    {
        public string DefaultMethod { get; set; } = "bearer"; // none, basic, bearer, oauth2, api_key
        public Dictionary<string, string> Credentials { get; set; } = new();
        public OAuth2Settings OAuth2 { get; set; } = new();
    }

    public class OAuth2Settings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AuthorizationUrl { get; set; }
        public string TokenUrl { get; set; }
        public string RedirectUrl { get; set; }
        public List<string> Scopes { get; set; } = new();
    }

    public class ApiSettings
    {
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryAttempts { get; set; } = 3;
        public bool EnableRateLimiting { get; set; } = true;
        public int RateLimitPerMinute { get; set; } = 60;
        public bool LogApiCalls { get; set; } = true;
    }

    public class ThirdPartyIntegrations
    {
        public List<ThirdPartyService> Services { get; set; } = new();
        public IntegrationMappings Mappings { get; set; } = new();
    }

    public class ThirdPartyService
    {
        public string ServiceName { get; set; }
        public string ServiceType { get; set; } // crm, erp, marketing, analytics
        public Dictionary<string, string> Configuration { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public string LastSyncDate { get; set; }
    }

    public class IntegrationMappings
    {
        public Dictionary<string, string> FieldMappings { get; set; } = new();
        public Dictionary<string, string> ValueMappings { get; set; } = new();
        public List<string> ExcludedFields { get; set; } = new();
    }

    public class DatabaseIntegrations
    {
        public List<DatabaseConnection> Connections { get; set; } = new();
        public DataSyncSettings SyncSettings { get; set; } = new();
    }

    public class DatabaseConnection
    {
        public string ConnectionName { get; set; }
        public string DatabaseType { get; set; } // mysql, postgresql, mssql, oracle
        public string ConnectionString { get; set; }
        public bool IsActive { get; set; } = true;
        public string Purpose { get; set; } // import, export, sync, backup
    }

    public class DataSyncSettings
    {
        public string SyncInterval { get; set; } = "daily"; // hourly, daily, weekly, manual
        public bool BidirectionalSync { get; set; } = false;
        public string ConflictResolution { get; set; } = "source_wins"; // source_wins, target_wins, manual
        public List<string> SyncTables { get; set; } = new();
    }

    public class FileIntegrations
    {
        public CloudStorageSettings CloudStorage { get; set; } = new();
        public FileProcessingSettings Processing { get; set; } = new();
    }

    public class CloudStorageSettings
    {
        public string Provider { get; set; } = "aws_s3"; // aws_s3, azure_blob, google_cloud
        public Dictionary<string, string> Credentials { get; set; } = new();
        public string DefaultBucket { get; set; }
        public string Region { get; set; }
    }

    public class FileProcessingSettings
    {
        public List<string> AllowedFileTypes { get; set; } = new();
        public long MaxFileSizeBytes { get; set; } = 10485760; // 10MB
        public bool AutoVirusScan { get; set; } = true;
        public bool AutoOptimizeImages { get; set; } = true;
    }

    public class IntegrationSettings
    {
        public int DefaultTimeout { get; set; } = 60;
        public bool EnableErrorNotifications { get; set; } = true;
        public string ErrorNotificationEmail { get; set; }
        public bool LogIntegrationActivity { get; set; } = true;
        public int LogRetentionDays { get; set; } = 90;
    }

    public class CustomBusinessRules
    {
        public List<BusinessRule> Rules { get; set; } = new();
        public BusinessRuleSettings Settings { get; set; } = new();
    }

    public class BusinessRule
    {
        public string RuleId { get; set; }
        public string RuleName { get; set; }
        public string Description { get; set; }
        public List<WorkflowCondition> Conditions { get; set; } = new();
        public List<BusinessAction> Actions { get; set; } = new();
        public string Priority { get; set; } = "medium"; // high, medium, low
        public bool IsActive { get; set; } = true;
        public DateTime EffectiveDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class BusinessAction
    {
        public string ActionType { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string ErrorHandling { get; set; } = "continue"; // stop, continue, retry
    }

    public class BusinessRuleSettings
    {
        public bool EnableRuleEngine { get; set; } = true;
        public int MaxRuleExecutionTime { get; set; } = 30; // seconds
        public bool LogRuleExecution { get; set; } = true;
        public string RuleConflictResolution { get; set; } = "priority"; // priority, first_match, all
    }

    public class CustomField
    {
        public string FieldId { get; set; }
        public string FieldName { get; set; }
        public string FieldType { get; set; } // text, number, date, boolean, select, multiselect
        public string Category { get; set; }
        public bool IsRequired { get; set; } = false;
        public string DefaultValue { get; set; }
        public FieldValidation Validation { get; set; } = new();
        public FieldDisplay Display { get; set; } = new();
        public List<FieldOption> Options { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }

    public class FieldValidation
    {
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public string Pattern { get; set; }
        public string CustomValidation { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class FieldDisplay
    {
        public string Label { get; set; }
        public string Placeholder { get; set; }
        public string HelpText { get; set; }
        public string Icon { get; set; }
        public string Width { get; set; } = "full"; // full, half, third, quarter
        public int Order { get; set; }
        public bool ShowInList { get; set; } = true;
        public bool ShowInForm { get; set; } = true;
    }

    public class FieldOption
    {
        public string Value { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string Icon { get; set; }
        public string Color { get; set; }
    }

    public class CustomizationSettings
    {
        public bool AllowUserCustomization { get; set; } = true;
        public List<string> LockedSettings { get; set; } = new();
        public CustomizationPermissions Permissions { get; set; } = new();
        public CustomizationLimits Limits { get; set; } = new();
        public BackupSettings Backup { get; set; } = new();
    }

    public class CustomizationPermissions
    {
        public bool CanChangeBranding { get; set; } = true;
        public bool CanChangeTheme { get; set; } = true;
        public bool CanChangeWorkflow { get; set; } = false;
        public bool CanAddCustomFields { get; set; } = true;
        public bool CanConfigureIntegrations { get; set; } = false;
        public List<string> RestrictedAreas { get; set; } = new();
    }

    public class CustomizationLimits
    {
        public int MaxCustomFields { get; set; } = 50;
        public int MaxWorkflowSteps { get; set; } = 20;
        public int MaxIntegrations { get; set; } = 10;
        public long MaxAssetSizeBytes { get; set; } = 52428800; // 50MB
        public int MaxApiEndpoints { get; set; } = 25;
    }

    public class BackupSettings
    {
        public bool AutoBackup { get; set; } = true;
        public string BackupInterval { get; set; } = "daily";
        public int BackupRetentionDays { get; set; } = 30;
        public bool BackupToCloud { get; set; } = true;
        public string BackupLocation { get; set; }
    }

    public class CustomizationMetadata
    {
        public string Version { get; set; } = "1.0.0";
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; }
        public List<CustomizationChange> ChangeHistory { get; set; } = new();
        public Dictionary<string, object> Tags { get; set; } = new();
        public string Description { get; set; }
    }

    public class CustomizationChange
    {
        public DateTime Timestamp { get; set; }
        public string ChangedBy { get; set; }
        public string ChangeType { get; set; } // created, updated, deleted
        public string Section { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> OldValue { get; set; } = new();
        public Dictionary<string, object> NewValue { get; set; } = new();
    }

    public class CustomizationTemplate
    {
        public string TemplateId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Industry { get; set; }
        public string CompanySize { get; set; }
        public SponsorCustomization Configuration { get; set; }
        public TemplateMetadata Metadata { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public TemplatePreview Preview { get; set; } = new();
        public bool IsPopular { get; set; }
        public bool IsRecommended { get; set; }
        public int UsageCount { get; set; }
        public double Rating { get; set; }
    }

    public class TemplateMetadata
    {
        public string Author { get; set; }
        public string Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public string License { get; set; }
        public List<string> CompatibleVersions { get; set; } = new();
    }

    public class TemplatePreview
    {
        public string ThumbnailUrl { get; set; }
        public List<string> ScreenshotUrls { get; set; } = new();
        public string DemoUrl { get; set; }
        public string VideoUrl { get; set; }
        public List<FeatureHighlight> Features { get; set; } = new();
    }

    public class FeatureHighlight
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string ImageUrl { get; set; }
    }

    public class WhiteLabelConfiguration
    {
        public bool Enabled { get; set; } = false;
        public string ProductName { get; set; }
        public string PoweredByText { get; set; }
        public bool HidePoweredBy { get; set; } = false;
        public CustomDomain Domain { get; set; } = new();
        public CustomEmailDomain EmailDomain { get; set; } = new();
        public LegalInformation Legal { get; set; } = new();
        public SupportConfiguration Support { get; set; } = new();
        public CustomDocumentation Documentation { get; set; } = new();
    }

    public class CustomDomain
    {
        public string Domain { get; set; }
        public bool SslEnabled { get; set; } = true;
        public string SslCertificate { get; set; }
        public bool CDNEnabled { get; set; } = true;
        public Dictionary<string, string> DnsRecords { get; set; } = new();
    }

    public class CustomEmailDomain
    {
        public string Domain { get; set; }
        public string DkimKey { get; set; }
        public string SpfRecord { get; set; }
        public string DmarcPolicy { get; set; }
        public bool IsVerified { get; set; } = false;
    }

    public class LegalInformation
    {
        public string CompanyName { get; set; }
        public string PrivacyPolicyUrl { get; set; }
        public string TermsOfServiceUrl { get; set; }
        public string CookiePolicyUrl { get; set; }
        public string DataProcessingAgreementUrl { get; set; }
        public string Copyright { get; set; }
        public List<string> Compliance { get; set; } = new(); // GDPR, CCPA, etc.
    }

    public class SupportConfiguration
    {
        public string SupportEmail { get; set; }
        public string SupportPhone { get; set; }
        public string SupportUrl { get; set; }
        public string DocumentationUrl { get; set; }
        public bool CustomSupportPortal { get; set; } = false;
        public string SupportPortalUrl { get; set; }
    }

    public class CustomDocumentation
    {
        public bool CustomDocumentationEnabled { get; set; } = false;
        public string DocumentationUrl { get; set; }
        public List<DocumentationSection> Sections { get; set; } = new();
        public DocumentationSettings Settings { get; set; } = new();
    }

    public class DocumentationSection
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }
        public int Order { get; set; }
        public bool IsPublic { get; set; } = true;
    }

    public class DocumentationSettings
    {
        public string Theme { get; set; } = "default";
        public bool SearchEnabled { get; set; } = true;
        public bool VersioningEnabled { get; set; } = false;
        public string Language { get; set; } = "tr";
    }

    public class CustomizationPreview
    {
        public string PreviewId { get; set; }
        public string PreviewUrl { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public PreviewOptions Options { get; set; } = new();
        public List<PreviewSection> Sections { get; set; } = new();
        public string Status { get; set; } = "ready";
    }

    public class CustomizationPreviewRequest
    {
        public BrandingConfiguration Branding { get; set; }
        public ThemeConfiguration Theme { get; set; }
        public List<string> PreviewSections { get; set; } = new(); // dashboard, login, forms, etc.
        public PreviewOptions Options { get; set; } = new();
    }

    public class PreviewOptions
    {
        public string ViewportSize { get; set; } = "desktop"; // mobile, tablet, desktop
        public bool IncludeAnimations { get; set; } = true;
        public bool IncludeInteractivity { get; set; } = false;
        public string Format { get; set; } = "html"; // html, pdf, image
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class PreviewSection
    {
        public string SectionName { get; set; }
        public string SectionUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Description { get; set; }
        public bool IsInteractive { get; set; }
    }

    public class CustomizationImport
    {
        public string Source { get; set; } // file, url, template
        public string Data { get; set; }
        public ImportOptions Options { get; set; } = new();
        public ImportValidation Validation { get; set; } = new();
    }

    public class ImportOptions
    {
        public bool OverwriteExisting { get; set; } = false;
        public bool ValidateBeforeImport { get; set; } = true;
        public bool BackupCurrent { get; set; } = true;
        public List<string> IncludeSections { get; set; } = new();
        public List<string> ExcludeSections { get; set; } = new();
    }

    public class ImportValidation
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public ImportSummary Summary { get; set; } = new();
    }

    public class ValidationError
    {
        public string Field { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; } = "error";
    }

    public class ValidationWarning
    {
        public string Field { get; set; }
        public string Message { get; set; }
        public string Recommendation { get; set; }
    }

    public class ImportSummary
    {
        public int TotalSections { get; set; }
        public int ValidSections { get; set; }
        public int InvalidSections { get; set; }
        public List<string> ImportedSections { get; set; } = new();
        public List<string> SkippedSections { get; set; } = new();
    }

    public class CustomizationAnalytics
    {
        public CustomizationUsage Usage { get; set; } = new();
        public UserEngagement Engagement { get; set; } = new();
        public PerformanceMetrics Performance { get; set; } = new();
        public CustomizationTrends Trends { get; set; } = new();
        public List<CustomizationInsight> Insights { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class CustomizationUsage
    {
        public int TotalCustomizations { get; set; }
        public int ActiveCustomizations { get; set; }
        public Dictionary<string, int> FeatureUsage { get; set; } = new();
        public Dictionary<string, int> SectionUsage { get; set; } = new();
        public string MostUsedFeature { get; set; }
        public string LeastUsedFeature { get; set; }
    }

    public class UserEngagement
    {
        public int CustomizationViews { get; set; }
        public int CustomizationUpdates { get; set; }
        public double AverageSessionDuration { get; set; }
        public double CustomizationCompletionRate { get; set; }
        public List<string> PopularSections { get; set; } = new();
        public UserSatisfaction Satisfaction { get; set; } = new();
    }

    public class UserSatisfaction
    {
        public double AverageRating { get; set; }
        public int TotalFeedback { get; set; }
        public Dictionary<string, int> RatingBreakdown { get; set; } = new();
        public List<string> CommonComplaints { get; set; } = new();
        public List<string> CommonPraises { get; set; } = new();
    }

    public class PerformanceMetrics
    {
        public double AverageLoadTime { get; set; }
        public double CustomizationImpact { get; set; }
        public int CacheHitRate { get; set; }
        public double ResourceUsage { get; set; }
        public List<PerformanceIssue> Issues { get; set; } = new();
    }

    public class PerformanceIssue
    {
        public string Issue { get; set; }
        public string Severity { get; set; }
        public string Impact { get; set; }
        public string Recommendation { get; set; }
    }

    public class CustomizationTrends
    {
        public List<TrendData> UsageTrends { get; set; } = new();
        public List<PopularCustomization> PopularCustomizations { get; set; } = new();
        public List<string> EmergingTrends { get; set; } = new();
        public SeasonalPatterns Seasonal { get; set; } = new();
    }

    public class TrendData
    {
        public DateTime Date { get; set; }
        public int CustomizationCount { get; set; }
        public int UpdateCount { get; set; }
        public Dictionary<string, int> FeatureUsage { get; set; } = new();
    }

    public class PopularCustomization
    {
        public string Feature { get; set; }
        public int UsageCount { get; set; }
        public double GrowthRate { get; set; }
        public string Category { get; set; }
    }

    public class SeasonalPatterns
    {
        public Dictionary<string, double> MonthlyPatterns { get; set; } = new();
        public Dictionary<string, double> WeeklyPatterns { get; set; } = new();
        public List<string> SeasonalEvents { get; set; } = new();
    }

    public class CustomizationInsight
    {
        public string InsightType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Impact { get; set; } // high, medium, low
        public string Recommendation { get; set; }
        public List<string> ActionItems { get; set; } = new();
        public double Confidence { get; set; }
    }

    #endregion
}
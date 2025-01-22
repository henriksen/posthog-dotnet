namespace PostHog;

/// <summary>
/// Extensions of <see cref="IPostHogClient"/> related to capturing events.
/// </summary>
public static class CaptureExtensions
{
    /// <summary>
    /// Captures an event.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="eventName">Human friendly name of the event. Recommended format [object] [verb] such as "Project created" or "User signed up".</param>
    /// <param name="properties">Optional: The properties to send along with the event.</param>
    public static void CaptureEvent(
        this IPostHogClient client,
        string distinctId,
        string eventName,
        Dictionary<string, object>? properties)
    {
        client = client ?? throw new ArgumentNullException(nameof(client));

        client.CaptureEvent(
            distinctId,
            eventName,
            properties,
            groups: null);
    }

    /// <summary>
    /// Captures a Page View ($pageview) event.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="pagePath">The URL or path of the page to capture.</param>
    /// <param name="properties">Additional context to save with the event.</param>
    public static void CapturePageView(
        this IPostHogClient client,
        string distinctId,
        string pagePath,
        Dictionary<string, object>? properties)
        => client.CaptureSpecialEvent(
            distinctId,
            eventName: "$pageview",
            eventPropertyName: "$current_url",
            eventPropertyValue: pagePath,
            properties);

    /// <summary>
    /// Captures a Page View ($pageview) event.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="pagePath">The URL or path of the page to capture.</param>
    public static void CapturePageView(
        this IPostHogClient client,
        string distinctId,
        string pagePath) => client.CapturePageView(distinctId, pagePath, properties: null);

    /// <summary>
    /// Captures a Screen View ($screen) event.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="screenName">The URL or path of the page to capture.</param>
    /// <param name="properties">Additional context to save with the event.</param>
    public static void CaptureScreenView(
        this IPostHogClient client,
        string distinctId,
        string screenName,
        Dictionary<string, object>? properties)
        => client.CaptureSpecialEvent(
            distinctId,
            eventName: "$screen",
            eventPropertyName: "$screen_name",
            eventPropertyValue: screenName,
            properties);

    /// <summary>
    /// Captures a Screen View ($screen) event.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="screenName">The URL or path of the page to capture.</param>
    public static void CaptureScreenView(
        this IPostHogClient client,
        string distinctId,
        string screenName) => client.CaptureScreenView(distinctId, screenName, properties: null);

    /// <summary>
    /// Captures a survey response.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="surveyId">The id of the survey.</param>
    /// <param name="surveyResponse">The survey response.</param>
    /// <param name="properties">Additional properties to capture.</param>
    public static void CaptureSurveyResponse(
        this IPostHogClient client,
        string distinctId,
        string surveyId,
        string surveyResponse,
        Dictionary<string, object>? properties)
        => client.CaptureSurveyResponses(
            distinctId,
            surveyId,
            surveyResponses: [surveyResponse],
            properties);

    /// <summary>
    /// Captures a survey response.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="surveyId">The id of the survey.</param>
    /// <param name="surveyResponses">The survey responses.</param>
    /// <param name="properties">Additional properties to capture.</param>
    public static void CaptureSurveyResponses(
        this IPostHogClient client,
        string distinctId,
        string surveyId,
        IReadOnlyList<string> surveyResponses,
        Dictionary<string, object>? properties)
    {
        client = client ?? throw new ArgumentNullException(nameof(client));
        surveyResponses = surveyResponses ?? throw new ArgumentNullException(nameof(surveyResponses));

        properties ??= new Dictionary<string, object>();
        properties["$survey_id"] = surveyId;

        if (surveyResponses.Count > 0)
        {
            properties["$survey_response"] = surveyResponses[0];
        }

        for (var i = 1; i < surveyResponses.Count; i++)
        {
            properties[$"survey_response_{i}"] = surveyResponses[i];
        }

        client.CaptureEvent(distinctId, "survey sent", properties, groups: null);
    }

    /// <summary>
    /// Captures that a survey was shown.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="surveyId">The id of the survey.</param>
    /// <param name="properties">Additional properties to capture.</param>
    public static void CaptureSurveyShown(
        this IPostHogClient client,
        string distinctId,
        string surveyId,
        Dictionary<string, object>? properties)
        => client.CaptureSpecialEvent(
            distinctId,
            eventName: "survey shown",
            eventPropertyName: "$survey_id",
            surveyId,
            properties);

    /// <summary>
    /// Captures that a survey was dismissed.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="surveyId">The id of the survey.</param>
    /// <param name="properties">Additional properties to capture.</param>
    public static void CaptureSurveyDismissed(
        this IPostHogClient client,
        string distinctId,
        string surveyId,
        Dictionary<string, object>? properties)
        => client.CaptureSpecialEvent(
            distinctId,
            eventName: "survey dismissed",
            eventPropertyName: "$survey_id",
            eventPropertyValue: surveyId,
            properties);

    static void CaptureSpecialEvent(
        this IPostHogClient client,
        string distinctId,
        string eventName,
        string eventPropertyName,
        string eventPropertyValue,
        Dictionary<string, object>? properties)
    {
        client = client ?? throw new ArgumentNullException(nameof(client));

        properties ??= new Dictionary<string, object>();
        properties[eventPropertyName] = eventPropertyValue;
        client.CaptureEvent(distinctId, eventName, properties);
    }
}
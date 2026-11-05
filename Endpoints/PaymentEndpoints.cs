using System.Security.Claims;
using System.Text;
using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Stripe;
using WellnessPlatform.Models;
using WellnessPlatform.Services;

namespace WellnessPlatform.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/payments/checkout/{appointmentId:int}",
                StartCheckoutAsync)
            .RequireAuthorization("ClientOnly");

        endpoints.MapPost("/payments/stripe/webhook", HandleStripeWebhookAsync)
            .AllowAnonymous();

        endpoints.MapPost(
                "/payments/facility/{appointmentId:int}",
                RecordFacilityPaymentAsync)
            .RequireAuthorization("PaymentsCollect");

        endpoints.MapPost(
                "/admin/payments/{paymentId:int}/refund",
                IssueRefundAsync)
            .RequireAuthorization("AdminOnly");

        endpoints.MapPost(
                "/admin/payments/refunds/{refundId:int}/retry",
                RetryRefundAsync)
            .RequireAuthorization("AdminOnly");

        return endpoints;
    }

    private static async Task<RedirectHttpResult> RecordFacilityPaymentAsync(
        int appointmentId,
        HttpContext context,
        IAntiforgery antiforgery,
        StripePaymentService payments,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("FacilityPayment");
        var destination = context.User.IsInRole(UserRole.Admin.ToString())
            ? "/admin/payments"
            : "/staff/payments";

        if (!await ValidateAntiforgeryAsync(
                context,
                antiforgery,
                logger,
                "facility payment"))
        {
            return TypedResults.Redirect($"{destination}?collection=form-expired");
        }

        if (!TryGetUserId(context, out var actorId))
        {
            return TypedResults.Redirect("/signin");
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        if (!string.Equals(
                form["Confirm"],
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.Redirect($"{destination}?collection=invalid");
        }

        try
        {
            await payments.RecordFacilityPaymentAsync(
                appointmentId,
                actorId,
                context.User.IsInRole(UserRole.Admin.ToString()),
                cancellationToken);
            return TypedResults.Redirect($"{destination}?collection=success");
        }
        catch (PaymentRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Facility payment could not be recorded for appointment {AppointmentId}.",
                appointmentId);
            return TypedResults.Redirect(
                $"{destination}?collection={exception.ResultCode}");
        }
    }

    private static async Task<RedirectHttpResult> IssueRefundAsync(
        int paymentId,
        HttpContext context,
        IAntiforgery antiforgery,
        StripePaymentService payments,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        const string destination = "/admin/payments";
        var logger = loggerFactory.CreateLogger("PaymentRefund");

        if (!await ValidateAntiforgeryAsync(
                context,
                antiforgery,
                logger,
                "payment refund"))
        {
            return TypedResults.Redirect($"{destination}?refund=form-expired");
        }

        if (!TryGetUserId(context, out var adminId))
        {
            return TypedResults.Redirect("/signin");
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        if (!string.Equals(
                form["Confirm"],
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.Redirect($"{destination}?refund=invalid");
        }

        decimal? amount = null;
        var amountText = form["Amount"].ToString();
        if (!string.IsNullOrWhiteSpace(amountText))
        {
            if (!decimal.TryParse(
                    amountText,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var parsedAmount))
            {
                return TypedResults.Redirect($"{destination}?refund=invalid");
            }

            amount = parsedAmount;
        }

        try
        {
            await payments.CreateRefundAsync(
                paymentId,
                adminId,
                amount,
                form["Reason"].ToString(),
                cancellationToken);
            return TypedResults.Redirect($"{destination}?refund=requested");
        }
        catch (PaymentRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Refund could not be requested for payment {PaymentId}.",
                paymentId);
            return TypedResults.Redirect(
                $"{destination}?refund={exception.ResultCode}");
        }
        catch (StripeException exception)
        {
            logger.LogError(
                exception,
                "Stripe refund status is uncertain for payment {PaymentId}.",
                paymentId);
            return TypedResults.Redirect($"{destination}?refund=uncertain");
        }
    }

    private static async Task<RedirectHttpResult> RetryRefundAsync(
        int refundId,
        HttpContext context,
        IAntiforgery antiforgery,
        StripePaymentService payments,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        const string destination = "/admin/payments";
        var logger = loggerFactory.CreateLogger("PaymentRefund");

        if (!await ValidateAntiforgeryAsync(
                context,
                antiforgery,
                logger,
                "refund status retry"))
        {
            return TypedResults.Redirect($"{destination}?refund=form-expired");
        }

        try
        {
            await payments.RetryRefundAsync(refundId, cancellationToken);
            return TypedResults.Redirect($"{destination}?refund=reconciled");
        }
        catch (PaymentRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Refund status could not be checked for refund request {RefundId}.",
                refundId);
            return TypedResults.Redirect(
                $"{destination}?refund={exception.ResultCode}");
        }
        catch (StripeException exception)
        {
            logger.LogError(
                exception,
                "Stripe refund status remains uncertain for refund request {RefundId}.",
                refundId);
            return TypedResults.Redirect($"{destination}?refund=uncertain");
        }
    }

    private static async Task<RedirectHttpResult> StartCheckoutAsync(
        int appointmentId,
        HttpContext context,
        IAntiforgery antiforgery,
        StripePaymentService payments,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("StripeCheckout");

        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException exception)
        {
            logger.LogWarning(
                exception,
                "Rejected an online payment request with an invalid antiforgery token.");
            return TypedResults.Redirect("/appointments?payment=form-expired");
        }

        var clientIdValue =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(clientIdValue, out var clientId))
        {
            return TypedResults.Redirect("/signin");
        }

        try
        {
            var checkoutUrl = await payments.CreateCheckoutUrlAsync(
                clientId,
                appointmentId,
                cancellationToken);
            return TypedResults.Redirect(checkoutUrl);
        }
        catch (PaymentRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Online payment could not be started for appointment {AppointmentId}.",
                appointmentId);
            return TypedResults.Redirect(
                $"/appointments?payment={exception.ResultCode}");
        }
        catch (StripeException exception)
        {
            logger.LogError(
                exception,
                "Stripe Checkout session creation failed for appointment {AppointmentId}.",
                appointmentId);
            return TypedResults.Redirect("/appointments?payment=unavailable");
        }
    }

    private static async Task<Results<Ok, BadRequest, StatusCodeHttpResult>>
        HandleStripeWebhookAsync(
            HttpRequest request,
            StripePaymentService payments,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("StripeWebhook");
        var webhookSecret = configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            logger.LogError(
                "Stripe webhook requests cannot be verified because Stripe:WebhookSecret is not configured.");
            return TypedResults.StatusCode(
                StatusCodes.Status503ServiceUnavailable);
        }

        var signature = request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature))
        {
            return TypedResults.BadRequest();
        }

        var body = await new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true)
            .ReadToEndAsync(cancellationToken);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                body,
                signature,
                webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException exception)
        {
            logger.LogWarning(
                exception,
                "Rejected a Stripe webhook with an invalid signature or payload.");
            return TypedResults.BadRequest();
        }

        await payments.ProcessWebhookAsync(
            stripeEvent,
            cancellationToken);

        return TypedResults.Ok();
    }

    private static async Task<bool> ValidateAntiforgeryAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        ILogger logger,
        string operation)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException exception)
        {
            logger.LogWarning(
                exception,
                "Rejected {Operation} request with an invalid antiforgery token.",
                operation);
            return false;
        }
    }

    private static bool TryGetUserId(HttpContext context, out int userId)
    {
        return int.TryParse(
            context.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}

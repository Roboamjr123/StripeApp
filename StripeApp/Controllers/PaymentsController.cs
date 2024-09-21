using DataLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;

namespace StripeApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly StripeSettings _stripeSettings;

        public PaymentsController(IOptions<StripeSettings> stripeOptions)
        {
            _stripeSettings = stripeOptions.Value;
        }

        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            var paymentIntentService = new PaymentIntentService();

            var options = new PaymentIntentCreateOptions
            {
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                Description = request.Description,
                ReceiptEmail = request.ReceiptEmail,
            };

            try
            {
                var paymentIntent = await paymentIntentService.CreateAsync(options);
                return Ok(new { 
                    ClientSecret = paymentIntent.ClientSecret });
            }
            catch (StripeException e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        [HttpGet("retrieve-payment-intent/{id}")]
        public async Task<IActionResult> RetrievePaymentIntent(string id)
        {
            var paymentIntentService = new PaymentIntentService();

            try
            {
                var paymentIntent = await paymentIntentService.GetAsync(id);
                return Ok(paymentIntent);
            }
            catch (StripeException e)
            {
                return NotFound(new { error = e.Message });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeSettings.WebhookSecret
            );

            if (stripeEvent.Type == Events.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                // Handle successful payment here
            }
            else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                // Handle failed payment here
            }

            return Ok();
        }
    }
}

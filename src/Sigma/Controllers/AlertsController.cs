using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Sigma.Controllers
{
    /// <summary>
    /// Simple in-memory management of alert subscriptions. This controller is
    /// deliberately lightweight and intended for demonstration purposes only.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        private static readonly List<AlertSubscription> _subscriptions = new();

        /// <summary>
        /// Get all current subscriptions.
        /// </summary>
        [HttpGet("subscriptions")]
        public IEnumerable<AlertSubscription> GetSubscriptions() => _subscriptions;

        /// <summary>
        /// Add a new subscription.
        /// </summary>
        [HttpPost("subscriptions")]
        public ActionResult<AlertSubscription> AddSubscription([FromBody] AlertSubscription request)
        {
            request.Id = Guid.NewGuid();
            _subscriptions.Add(request);
            return Ok(request);
        }

        /// <summary>
        /// Remove a subscription by id.
        /// </summary>
        [HttpDelete("subscriptions/{id}")]
        public IActionResult DeleteSubscription(Guid id)
        {
            var sub = _subscriptions.FirstOrDefault(s => s.Id == id);
            if (sub == null)
            {
                return NotFound();
            }
            _subscriptions.Remove(sub);
            return NoContent();
        }
    }

    /// <summary>
    /// Model describing a single subscription.
    /// </summary>
    public class AlertSubscription
    {
        public Guid Id { get; set; }
        public string? Endpoint { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;
using QrBot.Architecture;
using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Types;

namespace QrBot.Controllers
{
    [Route("/Bot/{action}/{token?}")]
    public class BotController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromRoute] string? token,
            [FromServices] BotFactory botFactory,
            [FromBody] [Required] Update update,
            CancellationToken cancellationToken)
        {
            if (token is null) return BadRequest();
            
            if (botFactory.Get(token) is { } bot)
            {
                await bot.UpdateHandler.HandleUpdateAsync(bot.Client, update, cancellationToken);
                return Ok();
            }

            return NotFound();
        }
    }
}

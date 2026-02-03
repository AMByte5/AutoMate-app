using AutoMate_app.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AutoMate_app.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        // GET: Chat
        public IActionResult Index()
        {
            return View();
        }

        // POST: Chat/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, error = "Message cannot be empty" });
            }

            try
            {
                var response = await _chatService.GetChatResponseAsync(request.Message, request.History);
                if (string.IsNullOrEmpty(response))
                {
                    return Json(new { success = false, error = "Received empty response from AI." });
                }
                return Json(new { success = true, response = response });
            }
            catch (Exception ex)
            {
                // Log the error (you can add logging here)
                return Json(new { success = false, error = $"Failed to get response: {ex.Message}" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
        public List<ChatMessage>? History { get; set; }
    }
}
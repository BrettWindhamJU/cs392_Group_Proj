using cs392_demo.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CS392_Demo3.Pages.Curriculum
{
    public class ChatbotModel : PageModel
    {
        private readonly AIService _ai;
        private readonly ILogger<ChatbotModel> _logger;

        public ChatbotModel(AIService ai, ILogger<ChatbotModel> logger)
        {
            _ai = ai;
            _logger = logger;
        }

        public class ChatMessage
        {
            public string Role { get; set; }  // "User" or "AI"
            public string Content { get; set; }
        }

        public List<ChatMessage> ChatHistory { get; set; } = new();

        [BindProperty]
        public string UserMessage { get; set; }

        public bool IsProcessing { get; private set; }

        public void OnGet()
        {
            // empty chat on first load
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsProcessing = true;

            try
            {
                if (string.IsNullOrWhiteSpace(UserMessage))
                {
                    ModelState.AddModelError(string.Empty, "Please enter a message.");
                    return Page();
                }

                // Add user message
                ChatHistory.Add(new ChatMessage
                {
                    Role = "User",
                    Content = UserMessage
                });

                // Call AI
                var response = await _ai.SendPromptAsync(UserMessage);

                // Add AI response
                ChatHistory.Add(new ChatMessage
                {
                    Role = "AI",
                    Content = response
                });

                // Clear input
                UserMessage = string.Empty;

                return Page();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Chatbot failed.");
                ModelState.AddModelError(string.Empty, "Error: " + ex.Message);
                return Page();
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}

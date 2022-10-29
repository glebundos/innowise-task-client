using innowise_task_server.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace innowise_task_client.ViewModels
{
    public class CreateFridgeVM
    {
        public Fridge Fridge { get; set; }

        public List<SelectListItem> Products { get; set; }

        public List<string> ProductsIDs { get; set; }
    }
}

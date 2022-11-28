using innowise_task_client.Models;
using innowise_task_client.ViewModels;
using innowise_task_server.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RestSharp;
using System.Diagnostics;
using System.Text.Json;

namespace innowise_task_client.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RestClient _restClient;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _restClient = new RestClient("http://localhost:5000/");
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var request = new RestRequest("api/Fridge");
            var response = await _restClient.GetAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
            
            var fridgeList = JsonSerializer.Deserialize<List<Fridge>>(response.Content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(fridgeList);
        }

        [HttpGet]
        public async Task<IActionResult> Fill()
        {
            var request = new RestRequest("api/FridgeProduct/fill");
            var response = await _restClient.GetAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            return RedirectToAction("Get");
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var request = new RestRequest("api/FridgeModel");
            var response = await _restClient.GetAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            var modelsList = JsonSerializer.Deserialize<List<FridgeModel>>(response.Content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            ViewData["Models"] = modelsList;

            
            //ViewData["Products"] = productsList;
            CreateFridgeVM createFridgeVM = new CreateFridgeVM();
            createFridgeVM.Products = await ProductsList();
            return View(createFridgeVM);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateFridgeVM fridgeVM)
        {
            if (fridgeVM.ProductsIDs == null)
            {
                return RedirectToAction("Create");
            }

            var request = new RestRequest("api/Fridge");
            var fridge = fridgeVM.Fridge;
            request.AddBody(new {Name = fridge.Name, OwnerName = fridge.OwnerName, ModelID = fridge.ModelID });
            var response = await _restClient.PostAsync(request);

            var createdFridge = JsonSerializer.Deserialize<Fridge>(response.Content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            fridgeVM.Products = await ProductsList();
            var fridgeProducts = new List<object>();
            List<SelectListItem> selectedItems = fridgeVM.Products.Where(p => fridgeVM.ProductsIDs.Contains(p.Value)).ToList();
            foreach (var product in selectedItems)
            {
                fridgeProducts.Add(new
                {
                    FridgeID = createdFridge.ID,
                    ProductID = Guid.Parse(product.Value),
                    Quantity = 0
                });
            }

            request = new RestRequest("api/FridgeProduct/many");
            request.AddBody(new { fridgeProducts = fridgeProducts });
            response = await _restClient.PostAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            return RedirectToAction("Get");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = new RestRequest($"api/Fridge/{id}");
            var response = await _restClient.GetAsync(request);
            var fridge = JsonSerializer.Deserialize<Fridge>(response.Content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            request = new RestRequest("api/FridgeModel");
            response = await _restClient.GetAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            var modelsList = JsonSerializer.Deserialize<List<FridgeModel>>(response.Content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            ViewData["Models"] = modelsList;

            if (fridge != null)
            {
                return View(fridge);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Fridge fridge)
        {
            var request = new RestRequest("api/Fridge");
            request.AddJsonBody(fridge);
            var response = await _restClient.PutAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            return RedirectToAction("Get");
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var request = new RestRequest($"api/Fridge");
            request.AddJsonBody(new { id  = id});
            var response = await _restClient.DeleteAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            return RedirectToAction("Get");
        }

        [HttpGet]
        public async Task<IActionResult> ListProducts(Guid id)
        {
            var request = new RestRequest($"api/FridgeProduct/fridge/{id}");
            var response = await _restClient.GetAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            var fProductsList = JsonSerializer.Deserialize<List<FridgeProduct>>(response.Content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(fProductsList);
        }

        [HttpGet, ActionName("EditProducts")]
        public async Task<IActionResult> EditProducts(Guid id)
        {
            var request = new RestRequest($"api/FridgeProduct/fridge/{id}");
            var response = await _restClient.GetAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            var fridgeProducts = JsonSerializer.Deserialize<List<FridgeProduct>>(response.Content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var productList = await ProductsList();
            ViewData["productList"] = productList;
            return View(fridgeProducts);
        }

        [HttpPost, ActionName("EditProductsConfirm")]
        public async Task<IActionResult> EditProductsConfirm(List<FridgeProduct> fridgeProducts, IFormCollection form)
        {
            var countBeforeEditing = fridgeProducts.Count();
            var countAfterEditing = countBeforeEditing + form["[0].ID"].Count() - 1;
            var diff = countAfterEditing - countBeforeEditing;

            List<object> newFridgeProducts = new List<object>();
            for (int i = 0; i < diff; i++)
            {
                var newFridgeId = Guid.Parse(form["[0].FridgeId"][i + 1]);
                var newProductId = Guid.Parse(form["[0].ProductId"][i + 1]);
                var newQuantity = int.Parse(form["[0].Quantity"][i + 1]);
                newFridgeProducts.Add(new
                {
                    FridgeID = newFridgeId,
                    ProductID = newProductId,
                    Quantity = newQuantity,
                });
            }

            var request = new RestRequest("api/FridgeProduct/many");
            request.AddBody(new { fridgeProducts = newFridgeProducts });
            var response = await _restClient.PostAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            List<FridgeProduct> oldFridgeProducts = new List<FridgeProduct>();
            for (int i = 0; i < countBeforeEditing; i++)
            {
                var oldId = Guid.Parse(form[$"[{i}].ID"][0]);
                var oldFridgeId = Guid.Parse(form[$"[{i}].FridgeId"][0]);
                var oldProductId = Guid.Parse(form[$"[{i}].ProductId"][0]);
                var oldQuantity = int.Parse(form[$"[{i}].Quantity"][0]);
                oldFridgeProducts.Add(new FridgeProduct
                {
                    ID = oldId,
                    FridgeID = oldFridgeId,
                    ProductID = oldProductId,
                    Quantity = oldQuantity,
                });
            }

            request = new RestRequest("api/FridgeProduct/many");
            request.AddBody(new { fridgeProducts = oldFridgeProducts });
            response = await _restClient.PutAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            Guid fridgeId = Guid.Parse(form["[0].FridgeID"].FirstOrDefault());
            return RedirectToAction("ListProducts", new {id = fridgeId});
        }

        [HttpPost, ActionName("DeleteProduct")]
        public async Task<IActionResult> DeleteProduct(Guid id, Guid fridgeId)
        {
            var request = new RestRequest($"api/FridgeProduct");
            request.AddBody(new { id = id });
            var response = await _restClient.DeleteAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            return RedirectToAction("EditProducts", new {id = fridgeId});
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<List<SelectListItem>> ProductsList()
        {
            var request = new RestRequest("api/Product");
            var response = await _restClient.GetAsync(request);

            if (!response.IsSuccessful)
            {
                return null;
            }

            var productsList = JsonSerializer.Deserialize<List<Product>>(response.Content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            List<SelectListItem> items = new List<SelectListItem>();
            foreach (var product in productsList)
            {
                items.Add(new SelectListItem
                {
                    Text = product.Name,
                    Value = product.ID.ToString()
                });
            }

            return items;
        }
    }
}
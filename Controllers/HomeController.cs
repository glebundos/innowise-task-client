using innowise_task_client.Models;
using innowise_task_client.ViewModels;
using innowise_task_server.Models;
using innowise_task_server.Services;
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
            var request = new RestRequest("api/Fridge/fill");
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
            var request = new RestRequest("api/Fridge/models");
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
            fridgeVM.Products = await ProductsList();
            var fridgeProducts = new List<FridgeProduct>();
            List<SelectListItem> selectedItems = fridgeVM.Products.Where(p => fridgeVM.ProductsIDs.Contains(p.Value)).ToList();
            foreach (var product in selectedItems)
            {
                fridgeProducts.Add(new FridgeProduct
                {
                    FridgeID = fridge.ID,
                    ProductID = Guid.Parse(product.Value),
                    Quantity = 0
                });
            }

            fridge.Products = fridgeProducts;
            request.AddJsonBody(fridge);

            var response = await _restClient.PostAsync(request);

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

            request = new RestRequest("api/Fridge/models");
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
            var request = new RestRequest($"api/Fridge/{id}");
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
            var request = new RestRequest($"api/Fridge/{id}/products");
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
            var request = new RestRequest($"api/Fridge/{id}/products");
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
            var id = form["[0].ID"];
            Guid fridgeId = Guid.Parse(form["[0].FridgeID"].FirstOrDefault());
            var productsIds = form["[0].ProductID"];
            var quantity = form["[0].Quantity"];

            for (int i = 0; i < productsIds.Count(); i++)
            {
                if (id[i] != " ") continue;
                FridgeProduct fp = new FridgeProduct();
                fp.ProductID = Guid.Parse(productsIds[i]);
                fp.Quantity = int.Parse(quantity[i]);
                fp.FridgeID = fridgeId;
                fridgeProducts.Add(fp);
            }

            var request = new RestRequest("api/Fridge/products");
            request.AddJsonBody(fridgeProducts);
            var response = await _restClient.PostAsync(request);

            if (!response.IsSuccessful)
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            return RedirectToAction("ListProducts", new {id = fridgeId});
        }

        [HttpPost, ActionName("DeleteProduct")]
        public async Task<IActionResult> DeleteProduct(Guid id, Guid fridgeId)
        {
            var request = new RestRequest($"api/Fridge/products/{id}");
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
            var request = new RestRequest("api/Fridge/products");
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
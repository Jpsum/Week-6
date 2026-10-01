using Microsoft.AspNetCore.Mvc;
using Bookstore.Models;
using System.Reflection;
using System.Xml.Linq;

namespace Bookstore.Controllers
{
    public class BookController : Controller
    {
        private const string XmlOutputRelativePath = "data/books.xml";
        private readonly IWebHostEnvironment _env;
        private Repository<Book> data { get; set; }
        public BookController(BookstoreContext ctx, IWebHostEnvironment env)
        {
            data = new Repository<Book>(ctx);
            _env = env;
        }

        public RedirectToActionResult Index() => RedirectToAction("List");

        public ViewResult List(BookGridData values)
        {
            // create options for querying books
            var options = new QueryOptions<Book> { 
                Includes = "Authors, Genre",
                OrderByDirection = values.SortDirection,
                PageNumber = values.PageNumber,
                PageSize = values.PageSize
            };
            if (values.IsSortByGenre) 
                options.OrderBy = b => b.GenreId;
            else if (values.IsSortByPrice) 
                options.OrderBy = b => b.Price;
            else 
                options.OrderBy = b => b.Title;

            // create view model
            var vm = new BookListViewModel { 
                Books = data.List(options),
                CurrentRoute = values,
                TotalPages = values.GetTotalPages(data.Count)
            };

            return View(vm);
        }

        public ViewResult Details(int id)
        {
            var book = data.Get(new QueryOptions<Book> {
                Where = b => b.BookId == id,
                Includes = "Authors, Genre"
            }) ?? new Book();
            return View(book);
        }

        [HttpPost]
        public RedirectToActionResult PageSize(BookGridData currentRoute)
        {
            return RedirectToAction("List", currentRoute.ToDictionary());
        }

        public RedirectToActionResult Export()
        {

            QueryOptions<Book> options = new QueryOptions<Book>
            {
                Includes = "Authors, Genre",
            };

            List<Book> books = (List<Book>)data.List(options);
            PropertyInfo[] bookProperties = typeof(Book)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string)
                    || p.PropertyType.IsValueType)
                .Where(p => p.Name != nameof(Book.GenreId))
                .ToArray();

            XElement xmlBooks = new XElement("Books"
                , books.Select( book =>
                    new XElement("Book", bookProperties.Select(prop =>
                            new XElement(prop.Name, prop.GetValue(book))
                    )
                    , new XElement("Genre", book.Genre?.Name ?? "Unknown")
                    , new XElement("Authors", book.Authors?.Select(author => 
                            new XElement("Author", author.FullName)) ?? new List<XElement>())
                    )));

            XDocument xmlDoc = new XDocument(xmlBooks);

            string path = Path.Combine(_env.WebRootPath, XmlOutputRelativePath);

            xmlDoc.Save(path);

            TempData["Message"] = "Export completed successfully.";

            return RedirectToAction("List");
        }
    }   
}
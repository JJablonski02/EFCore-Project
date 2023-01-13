using Microsoft.EntityFrameworkCore; // Include extension method
using Microsoft.EntityFrameworkCore.Infrastructure; // GetService extension method
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.ChangeTracking; // CollectionEntry
using Microsoft.EntityFrameworkCore.Storage; // IDbContextTransaction
using System.Collections.Generic;
using Packt.Shared;

using static System.Console;
using Microsoft.IdentityModel.Tokens;
using System.Data;

WriteLine($"Using {ProjectConstants.DatabaseProvider} database provider.");

//QueryingWithLike();
//QueryingProducts();
QueryingWithLike();
//FilteredIncludes();
QueryingCategories();

if(AddProduct(6, "Bob Burgers", 500M))
{
    WriteLine("Added new product");
}

if (IncreaseProductPrice(productNameStartsWith: "Bob", amount: 20M))
    {
    WriteLine("Update product price successful");
}

/*static int DeleteProducts(string productNameStartsWith)
{
    using (Northwind db = new())
    {
        IQueryable<Product>? products = db.Products?.Where(p => p.ProductName.Contains(productNameStartsWith));

        if (products is null)
        {
            WriteLine("No found products to delete");
            return 0;
        }
        else
        {
            db.Products.RemoveRange(products);
        }
        int affected = db.SaveChanges();
        return affected;
    }
}*/

static int DeleteProducts(string productNameStartsWith)
{
  using (Northwind db = new())
  {
    using (IDbContextTransaction t = db.Database.BeginTransaction())
    {
      WriteLine("Transaction isolation level: {0}",
        t.GetDbTransaction().IsolationLevel);

      IQueryable<Product>? products = db.Products?.Where(
        p => p.ProductName.StartsWith(productNameStartsWith));

      if (products is null)
      {
        WriteLine("No products found to delete.");
        return 0;
      }
      else
      {
        db.Products.RemoveRange(products);
      }

      int affected = db.SaveChanges();
      t.Commit();
      return affected;
    }
  }
}

int deleted = DeleteProducts("Burg");
WriteLine($"{deleted} products were deleted");

ListProducts();


static void QueryingProducts()
{
    using (Northwind db = new())
    {
        ILoggerFactory loggerFactory = db.GetService<ILoggerFactory>();
        loggerFactory.AddProvider(new ConsoleLoggerProvider());

        WriteLine("Products that cost more than a price, highest at top.");
        string? input;
        decimal price;

        do
        {
            Write("Enter a product price: ");
            input = ReadLine();
        } while (!decimal.TryParse(input, out price));

        IQueryable<Product>? products = db.Products?
          .Where(product => product.Cost > price)
          .OrderByDescending(product => product.Cost);

        if ((products is null) || (!products.Any()))
        {
            WriteLine("No products found.");
            return;
        }

        foreach (Product p in products)
        {
            WriteLine(
              "{0}: {1} costs {2:$#,##0.00} and has {3} in stock.",
              p.ProductId, p.ProductName, p.Cost, p.Stock);
        }
    }
}


static void QueryingCategories()
{
    using (Northwind db = new())
    {
        ILoggerFactory loggerFactory = db.GetService<ILoggerFactory>();
        loggerFactory.AddProvider(new ConsoleLoggerProvider());

        WriteLine("Categories and how many products they have:");

        // a query to get all categories and their related products 
        IQueryable<Category>? categories;
        // = db.Categories;
        //.Include(c => c.Products);

        db.ChangeTracker.LazyLoadingEnabled = false;

        Write("Enable eager loading? (Y/N): ");
        bool eagerloading = (ReadKey().Key == ConsoleKey.Y);
        bool explicitloading = false;
        WriteLine();

        if (eagerloading)
        {
            categories = db.Categories.Include(c => c.Products);
        }
        else
        {
            categories = db.Categories;

            Write("Enable explicit loading? (Y/N): ");
            eagerloading = (ReadKey().Key == ConsoleKey.Y);
            WriteLine();
            if ((categories is null) || (!categories.Any()))
            {
                WriteLine("No categories found.");
                return;
            }

            foreach (Category c in categories)
            {
                if (explicitloading)
                {
                    Write($"Explicitly load products for {c.CategoryName}? (Y/N): ");
                    ConsoleKeyInfo key = ReadKey();
                    WriteLine();
                    if (key.Key == ConsoleKey.Y)
                    {
                        CollectionEntry<Category, Product> products =
                          db.Entry(c).Collection(c2 => c2.Products);
                        if (!products.IsLoaded) products.Load();
                    }
                }
                WriteLine($"{c.CategoryName} has {c.Products.Count} products.");
            }
        }
    }
}
    static void FilteredIncludes()
    {
        using (Northwind db = new())
        {
            Write("Enter a minimum for units in stock: ");
            string unitInStock = ReadLine() ?? "10";
            int stock = int.Parse(unitInStock);

            IQueryable<Category>? categories = db.Categories
                .Include(c => c.Products.Where(p => p.Stock == stock));

            if ((categories is null) || (!categories.Any()))
            {
                WriteLine("No categories found.");
                return;
            }

            WriteLine($"ToQueryString: {categories.ToQueryString()}");

            foreach (Category c in categories)
            {
                WriteLine($"{c.CategoryName} has {c.Products.Count} products with a minimum of {stock} units in stock");

                foreach (Product p in c.Products)
                {
                    WriteLine($" {p.ProductName} has {p.Stock} units in stock.");
                }
            }
        }
    }

    static void QueryingWithLike()
{
    using (Northwind db = new Northwind())
    {
        ILoggerFactory loggerFactory = db.GetService<ILoggerFactory>();
        loggerFactory.AddProvider(new ConsoleLoggerProvider());

        Write("Enter part of the product name: ");
        string input = ReadLine();

        IQueryable<Product>? products = db.Products?
            .Where(p => EF.Functions.Like(p.ProductName, $"%{input}%"));

        if (products is null)
        {
            WriteLine("No products found");
            return;
        }

        foreach (Product p in products)
        {
            WriteLine("{0} has {1} units in stock. Discontinued? {2}",
                p.ProductName, p.Stock, p.Discontinued);
        }
    }
}

static bool AddProduct(int categoryId, string productName, decimal? price)
{
    using (Northwind db = new())
    {
        Product newproduct = new Product()
        {
            CategoryId = categoryId,
            ProductName = productName,
            Cost = price
        };
        db.Products.Add(newproduct);

        int affected = db.SaveChanges();
        return (affected == 1);
    }
}

static void ListProducts()
{
    using (Northwind db = new())
    {
        WriteLine("{0,-3} {1,-35} {2,8} {3,5} {4}",
            "ID", "Name", "Cost", "Stock", "Disc.");

        foreach (Product position in db.Products.OrderByDescending(product =>product.Cost))
        {
            WriteLine("{0:000} {1,-35} {2,8:$#,##0.00} {3,5} {4}",
                position.ProductId, position.ProductName, position.Cost, position.Stock, position.Discontinued);
        }
    }
}

static bool IncreaseProductPrice(string productNameStartsWith, decimal amount)
{
    using (Northwind db = new())
    {
        Product updateProduct = db.Products.First(p => p.ProductName.StartsWith(productNameStartsWith));

        updateProduct.Cost += amount;

        int affected = db.SaveChanges();
        return (affected == 1);
    }
}


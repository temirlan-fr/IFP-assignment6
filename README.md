Assignment 6
Exception Handling in Functional Programming

Author

Temirlan Bolat
IT-2504
Individual project

Project workflow

OrderApp is a reusable C# order processing module and console application. It loads a product catalog from JSON, looks up a requested product, validates an order, calculates its price and creates a new catalog with reduced stock. The application commits the new catalog only after success. Existing products and catalog snapshots remain unchanged.

The input is a JSON product array, a product ID and an integer quantity. The successful output contains the product ID, name, requested quantity and total price in KZT. Expected failures contain an error code and an explanation.

Requirements

Development SDK: .NET 10.0.401.
Target framework: .NET 10.
No external NuGet packages or database are required.
Run the following commands from the extracted repository root containing IFP-assignment6.sln.

Build

dotnet build IFP-assignment6.sln

Run all demonstrations

dotnet run --project OrderApp/OrderApp.csproj -- demo

Run automated tests

dotnet run --project OrderApp.Tests/OrderApp.Tests.csproj

The tests use a custom assertion runner permitted by the assignment. It compares actual and expected values, reports each result and returns exit code 1 on failure. Use the command above rather than dotnet test.

Normal order

dotnet run --project OrderApp/OrderApp.csproj -- order 1 2

Expected result: Keyboard, quantity 2, total 30000.00 KZT.

Invalid quantity

dotnet run --project OrderApp/OrderApp.csproj -- order 1 0

Expected error: INVALID_QUANTITY. Quantity must be greater than zero and the message includes the supplied quantity.

Inactive product

dotnet run --project OrderApp/OrderApp.csproj -- order 2 1

Expected error: PRODUCT_INACTIVE. Mouse is not available for sale.

Insufficient stock

dotnet run --project OrderApp/OrderApp.csproj -- order 1 6

Expected error: INSUFFICIENT_STOCK. The message identifies Keyboard, the requested quantity and available stock.

Missing required product

dotnet run --project OrderApp/OrderApp.csproj -- order 999 1

Expected error: PRODUCT_NOT_FOUND. The message identifies product ID 999.

Load a valid catalog

dotnet run --project OrderApp/OrderApp.csproj -- load sample-data/products.json

Expected result: three products loaded.

Malformed file

dotnet run --project OrderApp/OrderApp.csproj -- load sample-data/malformed.json

Expected error: MALFORMED_JSON with the operation, file path and parsing details.

Missing file and optional lookups

The demo command creates a controlled temporary directory and attempts to read a file that does not exist. It reports FILE_NOT_FOUND and removes the temporary directory afterwards. This demonstration does not depend on a missing file on the student's computer.

The demo also displays a present product, an absent product, present stock of zero and a present inactive status of false. Missing lookup values are displayed as absent. No fabricated product or default primitive value is returned.

Standalone order and load commands return exit code 0 on success and 1 on a reported failure. Invalid command syntax returns 2. Demo returns 0 after showing the intentionally failing scenarios; failure to load its initial catalog returns 1.

State behaviour

The demo uses one application instance. Ordering two keyboards changes stock from five to three. Subsequent rejected orders and failed reloads preserve that catalog. Ordering the exact available stock is allowed; a subsequent order is rejected. Each standalone command starts a new process and reads the initial sample catalog. Stock changes are in memory only. The program does not write product files.

Task 1 Define explicit domain outcomes

OrderApp/Result.cs defines Result, Success, Failure and AppError. Success has a required payload and Failure has a useful error without a success payload. Constructor guards reject null payloads and null errors as programming mistakes. Ordinary business failures return Failure instead of throwing exceptions.

OrderApp/OrderService.cs CreateOrder checks quantity, active status, available stock and the supported total amount in that order. It returns the first failing rule. The three main failures are INVALID_QUANTITY, PRODUCT_INACTIVE and INSUFFICIENT_STOCK. The additional TOTAL_TOO_LARGE error prevents numeric overflow for extreme input prices and quantities. An exact scaled integer comparison checks this boundary before multiplying decimals.

OrderApp/Models.cs defines immutable Product, Order and Checkout records. CreateOrder validates and calculates an order without changing inventory. PlaceOrder additionally prepares an updated catalog.

Task 2 Model missing values

OrderApp/Option.cs defines Some and None. Some zero and Some false are valid present values. Catalog.Find, FindStock and FindActiveStatus in OrderApp/Catalog.cs perform real catalog lookups. None is never inferred from default values.

A product is required for ordering. Option.ToResult converts its absence into PRODUCT_NOT_FOUND. Lookup display is optional and explicitly prints absent. Option represents presence, Result represents an operation outcome and Product.IsActive represents the product's sale state. A valid inactive product is different from invalid input or an absent product.

Task 3 Implement and use Map and Bind

Result.Map takes a pure transformation and wraps its return value in Success. Result.Bind takes an operation returning Result and returns that result directly. Both preserve the same AppError on failure and skip their callbacks.

OrderService.PlaceOrder uses Find, ToResult, Bind and Map. Bind calls CreateOrder because order validation may fail. Map constructs Checkout and a new stock snapshot after success. A missing product stops validation and stock calculation. Program also composes file loading and PlaceOrder through Bind, then maps the checkout to the order. PrintResult handles the final display outcome at the console boundary.

Task 4 Handle operational exceptions at boundaries

OrderApp/CatalogFile.cs Load owns file access and JSON parsing. It handles missing files and directories, malformed JSON, denied access, I/O errors and invalid paths. Errors identify the load operation and path. There is no silent fallback.

Every JSON product must include Id, Name, Price, Stock and IsActive with the exact property names shown in sample-data/products.json. IDs must be unique positive integers. Names must be nonempty. Prices and stocks must be nonnegative. Missing fields and wrong JSON types return MALFORMED_JSON. Null catalogs, null rows, duplicate IDs or invalid field values return INVALID_CATALOG. An empty JSON array is a valid empty catalog and lookups return None.

Loading builds a candidate catalog locally and returns Success only after all rows pass validation. OrderApplication.Reload swaps the current catalog only on Success. A parsing or validation failure after a valid first row cannot create a partial update. Product files are read only.

Task 5 Integrate outcomes into the application

OrderApp/OrderApplication.cs owns the current in-memory catalog. Submit commits only successful Checkout results. Reload commits only a completely validated catalog. Domain operations contain no file access.

OrderApp/Program.cs runs the normal path, all three main domain failures, present and missing lookups, zero and false lookups, a required missing product, missing file and malformed file. It prints whether state was preserved after each file failure.

Automated tests and expected results

OrderApp.Tests/Program.cs retains the initial seven tests. OrderApp.Tests/WorkflowTests.cs adds the integrated checks. All file fixtures are created inside unique temporary directories owned by the tests and are removed by IDisposable even if assertions fail. Tests do not rely on local sample files.

Valid order: all order fields match and total is 30000 KZT.
Zero quantity: INVALID_QUANTITY and the exact useful message.
Negative quantity: INVALID_QUANTITY and the exact useful message.
Inactive product: PRODUCT_INACTIVE with the product name.
Insufficient stock: INSUFFICIENT_STOCK with requested and available quantities.
Exact stock order: quantity five succeeds with total 75000 KZT.
Rejected order: the original product remains unchanged.
Present lookup: Keyboard is returned.
Absent lookup: None is returned.
Zero stock: Some zero differs from None.
False active status: Some false differs from None.
Required missing product: PRODUCT_NOT_FOUND identifies ID 999.
Map success: callback receives four and returns twelve.
Bind success: callback receives four and its exact Result is returned.
Bind rejection: the exact failure returned by the next operation is preserved.
Failure propagation: Map and Bind preserve the same error and callback count stays zero.
Missing lookup propagation: validation and calculation callback count stays zero.
Integrated order chain: total is 30000, new stock is three and original stock remains five.
Application domain failures: successful stock change is committed and all later failures preserve the same catalog.
Stock exhaustion: exact-stock order succeeds and the next order is rejected.
Free product: zero total is a successful value.
Excessive total: TOTAL_TOO_LARGE preserves the application state.
File-to-order chain: a temporary JSON file produces total 30000 through Bind and Map.
Decimal boundary: a rounded division cannot hide overflow and an ordinary fractional price calculates correctly.
Missing file: FILE_NOT_FOUND identifies the path and preserves the catalog without creating a file.
Missing directory: FILE_NOT_FOUND is returned.
Malformed JSON after a valid row: MALFORMED_JSON preserves the catalog and source content.
Invalid data after a valid row: INVALID_CATALOG preserves the catalog and source content.
Duplicate IDs: INVALID_CATALOG is returned.
Missing required fields: MALFORMED_JSON is returned instead of accepting default data.
Null catalog and null row: INVALID_CATALOG is returned for both.
Empty catalog: successful empty catalog and explicit None lookup.
Successful reload: the complete replacement catalog is committed and old products disappear.

Latest verification

On November 4, 2026, the solution built with zero warnings and zero errors. All 33 automated tests passed. The integrated demo produced the expected outcomes and preserved state after both file failures. Eight additional command line checks verified the documented output and exit codes for success, domain failures, valid and malformed file loading and invalid command syntax.

The documented build, demo and test commands also passed from a clean extraction of the submission archive.

Individual work

Temirlan Bolat, IT-2504. This individual submission contains the Result and Option models, catalog lookups, order rules, composition pipeline, JSON boundary, application integration, automated tests and documentation. The implementation was prepared with AI assistance. The student must review and explain the submitted code during the individual defense.

Defense walkthrough

Run demo to show success, domain failure, missing lookup and file failure. Open OrderService.PlaceOrder to trace Find, ToResult, Bind and Map. Open the failure propagation test to show that both callback counters remain zero. Explain Some zero and Some false using FindStock and FindActiveStatus. Open CatalogFile.Load and OrderApplication.Reload to explain why a failed load cannot replace the current catalog. Modify a domain rule or its test and rerun the assertion runner.

Submission

The repository includes the solution, both projects, C# sources, README and sample JSON files. The local artifacts/Assignment6.zip is a clean submission snapshot that excludes bin, obj, Git data and IDE caches. Extract it and run the build, demo and test commands from its root. Generated archives are excluded from Git. The archive must be uploaded to Moodle separately; pushing to GitHub does not submit it.

Repository

https://github.com/temirlan-fr/IFP-assignment6

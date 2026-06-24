using System.Xml.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var dir = AppContext.BaseDirectory;
var xmlFiles = Directory.GetFiles(dir, "*.xml");

if (xmlFiles.Length == 0)
{
    Console.WriteLine("执行目录下没有找到 XML 文件。");
    return;
}

foreach (var xmlFile in xmlFiles)
{
    try
    {
        var doc = XDocument.Load(xmlFile);
        var data = InvoiceParser.Extract(doc);

        var pdfFile = Path.ChangeExtension(xmlFile, ".pdf");
        new InvoicePdf(data).GeneratePdf(pdfFile);

        Console.WriteLine($"成功生成: {pdfFile}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"转换失败: {xmlFile}");
        Console.WriteLine(ex.Message);
    }
}

public class InvoiceData
{
    public string InvoiceNumber { get; set; } = "";
    public string InvoiceDate { get; set; } = "";
    public string DueDate { get; set; } = "";
    public string BuyerReference { get; set; } = "";
    public string PurchaseOrder { get; set; } = "";
    public string PaymentTerms { get; set; } = "";
    public string Currency { get; set; } = "";

    public Party Seller { get; set; } = new();
    public Party Buyer { get; set; } = new();
    public Shipment Shipment { get; set; } = new();
    public Transfer Transfer { get; set; } = new();

    public List<ProductLine> Products { get; set; } = new();

    public string Subtotal { get; set; } = "";
    public string Tax { get; set; } = "";
    public string Total { get; set; } = "";
}

public class Party
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string Country { get; set; } = "";
    public string Email { get; set; } = "";
}

public class Shipment
{
    public string Date { get; set; } = "";
    public string Name { get; set; } = "";
    public string LineOne { get; set; } = "";
    public string PostcodeCode { get; set; } = "";
    public string CityName { get; set; } = "";
    public string CountryID { get; set; } = "";
}

public class Transfer
{
    public string AccountName { get; set; } = "";
    public string IBANID { get; set; } = "";
    public string BICID { get; set; } = "";
}

public class ProductLine
{
    public string LineID { get; set; } = "";
    public string SellerAssignedID { get; set; } = "";
    public string Model { get; set; } = "";
    public string Content { get; set; } = "";
    public string Description { get; set; } = "";
    public string Quantity { get; set; } = "";
    public string UnitPrice { get; set; } = "";
    public string Total { get; set; } = "";
}

public static class InvoiceParser
{
    public static InvoiceData Extract(XDocument doc)
    {
        var rootNs = doc.Root?.Name.NamespaceName ?? "";

        if (rootNs.Contains("oasis:names:specification:ubl:schema:xsd:Invoice-2"))
            return ExtractUbl(doc);

        if (rootNs.Contains("uncefact:data:standard:CrossIndustryInvoice"))
            return ExtractCii(doc);

        var hasUbl = doc.Descendants().Any(e =>
            e.Name.LocalName == "AccountingSupplierParty" ||
            e.Name.LocalName == "InvoiceLine");

        if (hasUbl)
            return ExtractUbl(doc);

        throw new Exception("无法识别 XML 类型，不是 UBL Invoice-2 或 CII CrossIndustryInvoice。");
    }

    private static string Text(XElement? node, string localName)
    {
        return node?
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == localName)?
            .Value
            .Trim() ?? "";
    }

    private static string DirectText(XElement? node, string localName)
    {
        return node?
            .Elements()
            .FirstOrDefault(e => e.Name.LocalName == localName)?
            .Value
            .Trim() ?? "";
    }

    private static XElement? First(XElement? node, string localName)
    {
        return node?
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == localName);
    }

    private static InvoiceData ExtractUbl(XDocument doc)
    {
        var root = doc.Root!;
        var data = new InvoiceData();

        data.InvoiceNumber = root.Elements().FirstOrDefault(e => e.Name.LocalName == "ID")?.Value ?? "";
        data.InvoiceDate = Text(root, "IssueDate");
        data.DueDate = Text(root, "DueDate");
        data.BuyerReference = Text(root, "BuyerReference");
        data.Currency = Text(root, "DocumentCurrencyCode");

        var orderRef = First(root, "OrderReference");
        data.PurchaseOrder = Text(orderRef, "ID");
        data.PaymentTerms = Text(root, "Note");

        var supplier = First(root, "AccountingSupplierParty");
        var supplierParty = First(supplier, "Party");
        data.Seller = ParseUblParty(supplierParty);

        var customer = First(root, "AccountingCustomerParty");
        var customerParty = First(customer, "Party");
        data.Buyer = ParseUblParty(customerParty);

        var delivery = First(root, "Delivery");
        var location = First(delivery, "DeliveryLocation");
        var address = First(location, "Address");
        var deliveryParty = First(delivery, "DeliveryParty");

        data.Shipment = new Shipment
        {
            Date = Text(delivery, "ActualDeliveryDate"),
            Name = Text(deliveryParty, "Name"),
            LineOne = Text(address, "StreetName"),
            PostcodeCode = Text(address, "PostalZone"),
            CityName = Text(address, "CityName"),
            CountryID = Text(address, "IdentificationCode")
        };

        if (string.IsNullOrWhiteSpace(data.Shipment.Name))
            data.Shipment.Name = data.Buyer.Name;

        foreach (var line in root.Descendants().Where(e => e.Name.LocalName == "InvoiceLine"))
        {
            var item = First(line, "Item");
            var price = First(line, "Price");

            data.Products.Add(new ProductLine
            {
                LineID = DirectText(line, "ID"),
                SellerAssignedID = Text(First(item, "SellersItemIdentification"), "ID"),
                Model = Text(item, "Name"),
                Description = Text(item, "Description"),
                Quantity = DirectText(line, "InvoicedQuantity"),
                UnitPrice = Text(price, "PriceAmount"),
                Total = DirectText(line, "LineExtensionAmount")
            });
        }

        var monetary = First(root, "LegalMonetaryTotal");
        data.Subtotal = Text(monetary, "LineExtensionAmount");
        data.Tax = Text(First(root, "TaxTotal"), "TaxAmount");
        data.Total = Text(monetary, "PayableAmount");

        var paymentMeans = First(root, "PaymentMeans");
        var account = First(paymentMeans, "PayeeFinancialAccount");
        var branch = First(account, "FinancialInstitutionBranch");

        data.Transfer = new Transfer
        {
            AccountName = Text(account, "Name"),
            IBANID = Text(account, "ID"),
            BICID = Text(branch, "ID")
        };

        return data;
    }

    private static Party ParseUblParty(XElement? party)
    {
        var address = First(party, "PostalAddress");
        var contact = First(party, "Contact");

        return new Party
        {
            Name = Text(First(party, "PartyName"), "Name"),
            Address = Text(address, "StreetName"),
            City = Text(address, "CityName"),
            Postcode = Text(address, "PostalZone"),
            Country = Text(First(address, "Country"), "IdentificationCode"),
            Email = Text(contact, "ElectronicMail")
        };
    }

    private static InvoiceData ExtractCii(XDocument doc)
    {
        var root = doc.Root!;
        var data = new InvoiceData();

        var exchanged = First(root, "ExchangedDocument");

        data.InvoiceNumber = Text(exchanged, "ID");
        data.InvoiceDate = Text(First(exchanged, "IssueDateTime"), "DateTimeString");
        data.DueDate = Text(First(root, "DueDateDateTime"), "DateTimeString");
        data.BuyerReference = Text(root, "BuyerReference");
        data.PurchaseOrder = Text(First(root, "BuyerOrderReferencedDocument"), "IssuerAssignedID");
        data.PaymentTerms = Text(First(root, "SpecifiedTradePaymentTerms"), "Description");
        data.Currency = Text(root, "InvoiceCurrencyCode");

        var seller = First(root, "SellerTradeParty");
        var buyer = First(root, "BuyerTradeParty");
        var shipment = First(root, "ShipToTradeParty");

        data.Seller = ParseCiiParty(seller);
        data.Buyer = ParseCiiParty(buyer);

        data.Shipment = new Shipment
        {
            Date = Text(First(root, "OccurrenceDateTime"), "DateTimeString"),
            Name = DirectText(shipment, "Name"),
            LineOne = Text(shipment, "LineOne"),
            PostcodeCode = Text(shipment, "PostcodeCode"),
            CityName = Text(shipment, "CityName"),
            CountryID = Text(shipment, "CountryID")
        };

        foreach (var item in root.Descendants().Where(e => e.Name.LocalName == "IncludedSupplyChainTradeLineItem"))
        {
            data.Products.Add(new ProductLine
            {
                LineID = Text(item, "LineID"),
                SellerAssignedID = Text(item, "SellerAssignedID"),
                Model = Text(item, "Name"),
                Content = Text(item, "Content"),
                Description = Text(item, "Description"),
                Quantity = Text(item, "BilledQuantity"),
                UnitPrice = Text(item, "ChargeAmount"),
                Total = Text(item, "LineTotalAmount")
            });
        }

        var totals = First(root, "SpecifiedTradeSettlementHeaderMonetarySummation");

        data.Subtotal = Text(totals, "LineTotalAmount");
        data.Tax = Text(totals, "TaxTotalAmount");
        data.Total = Text(totals, "GrandTotalAmount");

        var transfer = First(root, "SpecifiedTradeSettlementPaymentMeans");

        data.Transfer = new Transfer
        {
            AccountName = Text(First(transfer, "PayeePartyCreditorFinancialAccount"), "AccountName"),
            IBANID = Text(First(transfer, "PayeePartyCreditorFinancialAccount"), "IBANID"),
            BICID = Text(First(transfer, "PayeeSpecifiedCreditorFinancialInstitution"), "BICID")
        };

        return data;
    }

    private static Party ParseCiiParty(XElement? party)
    {
        return new Party
        {
            Name = DirectText(party, "Name"),
            Address = Text(party, "LineOne"),
            City = Text(party, "CityName"),
            Postcode = Text(party, "PostcodeCode"),
            Country = Text(party, "CountryID"),
            Email = Text(party, "URIID")
        };
    }
}

public class InvoicePdf : IDocument
{
    private readonly InvoiceData data;

    public InvoicePdf(InvoiceData data)
    {
        this.data = data;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Content().Column(col =>
            {
                col.Item().AlignRight().Text("Invoice").FontSize(32).Bold();

                col.Item().PaddingVertical(10).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Invoice number: {data.InvoiceNumber}").Bold();
                        c.Item().Text($"Place Date: {data.InvoiceDate}");
                        c.Item().Text($"Due date: {data.DueDate}");
                        c.Item().Text($"Purchase order: {data.PurchaseOrder}");
                        c.Item().Text($"BuyerReference: {data.BuyerReference}");
                        c.Item().Text($"Currency: {data.Currency}");
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Date of delivery: {data.Shipment.Date}").Bold();
                        c.Item().Text(data.Shipment.Name).Bold();
                        c.Item().Text($"Street/house Nr.: {data.Shipment.LineOne}");
                        c.Item().Text($"Code postal: {data.Shipment.PostcodeCode}");
                        c.Item().Text($"Place: {data.Shipment.CityName}");
                        c.Item().Text($"Country: {data.Shipment.CountryID}");
                    });
                });

                col.Item().PaddingVertical(10).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Seller: {data.Seller.Name}").Bold();
                        c.Item().Text($"{data.Seller.Address}, {data.Seller.City}");
                        c.Item().Text($"{data.Seller.Postcode}, {data.Seller.Country}");
                        c.Item().Text($"Email: {data.Seller.Email}");
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Buyer: {data.Buyer.Name}").Bold();
                        c.Item().Text($"{data.Buyer.Address}, {data.Buyer.City}");
                        c.Item().Text($"{data.Buyer.Postcode}, {data.Buyer.Country}");
                        c.Item().Text($"Email: {data.Buyer.Email}");
                    });
                });

                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(45);
                        columns.RelativeColumn(4);
                        columns.ConstantColumn(55);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(70);
                    });

                    Header(table, "LineID");
                    Header(table, "model and Description");
                    Header(table, "Quantity");
                    Header(table, "Unit Price");
                    Header(table, "Total");

                    foreach (var p in data.Products)
                    {
                        Cell(table, p.LineID);
                        Cell(table, $"{p.Model}\n{p.Description}\n{p.Content}\n{p.SellerAssignedID}");
                        Cell(table, p.Quantity);
                        Cell(table, $"{p.UnitPrice} {data.Currency}");
                        Cell(table, $"{p.Total} {data.Currency}");
                    }
                });

                col.Item().PaddingTop(12).AlignRight().Column(c =>
                {
                    c.Item().Text($"Subtotal: {data.Subtotal}");
                    c.Item().Text($"VAT: {data.Tax}");
                    c.Item().Text($"Total amount: {data.Total}").Bold();
                });

                col.Item().PaddingTop(12).Column(c =>
                {
                    c.Item().Text($"payment: {data.PaymentTerms}").Bold();
                    c.Item().Text("");
                    c.Item().Text($"Account: {data.Transfer.AccountName}");
                    c.Item().Text($"IBAN: {data.Transfer.IBANID}");
                    c.Item().Text($"BIC: {data.Transfer.BICID}");
                });
            });
        });
    }

    private static void Header(TableDescriptor table, string text)
    {
        table.Cell()
            .Border(1)
            .Padding(4)
            .AlignCenter()
            .Text(text)
            .Bold();
    }

    private static void Cell(TableDescriptor table, string text)
    {
        table.Cell()
            .Border(1)
            .Padding(4)
            .Text(text ?? "");
    }
}
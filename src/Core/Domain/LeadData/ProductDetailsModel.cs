using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.LeadData;
public class ProductDetailsModel : AuditableEntity, IAggregateRoot
{
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public Guid? VendorId { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Tax { get; set; }
    public bool? Taxable { get; set; }
    //public string CompanyId { get; set; }
    //public int IsDeleted { get; set; }

    public string? Description { get; set; }

    public ProductDetailsModel(string? productName, string? productCode, Guid? vendorId, decimal? unitPrice, string? tax, bool? taxable, string? description)
    {
        ProductName = productName;
        ProductCode = productCode;
        VendorId = vendorId;
        UnitPrice = unitPrice;
        Tax = tax;
        Taxable = taxable;
        Description = description;
    }

    public ProductDetailsModel Update(string? productName, string? productCode, Guid? vendorId, decimal? unitPrice, string? tax, bool? taxable, string? description)
    {
        if (productName is not null && ProductName?.Equals(productName) is not true) ProductName = productName;
        if (productCode is not null && ProductCode?.Equals(productCode) is not true) ProductCode = productCode;
        if (vendorId != Guid.Empty && !VendorId.Equals(vendorId)) VendorId = vendorId;
        if (UnitPrice != unitPrice) UnitPrice = unitPrice;
        if (tax is not null && Tax?.Equals(tax) is not true) Tax = tax;
        if (Taxable != taxable) Taxable = taxable;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        return this;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Product;
public class ProductDtoData
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public Guid? VendorId { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Tax { get; set; }
    public bool? Taxable { get; set; }
    public string? Description { get; set; }
}

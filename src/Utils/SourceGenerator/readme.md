Following the suggestions of Mukesh

Should be under src/Utils/SourceGenerator
with assembly name as FSH.WebApi.SourceGenerator

The code is based on the product and brand entities
I used the same aproach as I did some years ago so bear with me should the whole thing is well 'not that modern' .
But at the end of the 'run' all the files are created.
After a rerun fullstackhero sets the permissions (as it does now) .

with the new directories in place (and if not completely integrated in fullstackhero)
Add a reference to the Utils project in the host project

in program.cs
Add 'using FSH.WebApi.Utils;'
Add 'builder.Services.AddUtils(builder.Configuration);'
Add 'await app.Services.InitializeGenerateSources();' ' 

approach : 
I transformed the classes to text files.
the entityrelated words are replaced with specific "terms"
With System.IO when creating the files the terms are replaced with the new entityrelated words.
(in i.e. productdto product is replaced with <&Entity&>  : <& will not be accepted when creating a new entity )
A list of the <& naming at the bottom of this page
All these files reside in a subfolder of Sourcegenerator => Basicsources
These text files are based on the brand and product ones (The image in product.cs is left out in the newly created class).
The different classes to generate the sourcefiles reside in SourceGenerator => SourceGeneratorClasses
The main program is GenerateSources
To give developers the opportunity to change foldernames etc There is a settings file called GenerateSourceSettings.cs.
First I had a json file but the sourcesettings.cs was needed anyway so I skipped json and added the settings values directly.
How it works.

After the creation of a new entity (see product.cs or brand.cs) Add a migration, run it and update database.
Due to Localization issues i Left out the message T[] ( I didn't want to meke all the resx files')
If you restart the program 
If => await app.Services.InitializeGenerateSources(); is commented out  in program.cs it does not run
A secondary check is in the generatesourcessettings.cs.

The code will get the entityTypes from EF-Core
and will foreach through the entitylist and check if it is one of the entities in created in FSH.WebApi.Domain.Catalog.
Those are the entities we are interested in.
Then the code will check if the directory for the entity exists in : FSH.WebApi.Application.Catalog
If not the sources are generated.
the permission file is adapted
the controllers are made.

for the validators a validation is made with the validationrule for the first property in our made entity.
Obviously the business specific validation rules are to be made by the developer.

If any alterations are made to an entity and a new migration is run, delete the appropriate entity folder in  FSH.WebApi.Application.Catalog. 
Restart the program and the altered sourcefiles are made.
the same goes if one of the class code is changed the txtfile in the basicsources and SourceCodeGeneratorClasses folders
need to be adapted then delete the contents of the  entities folder in  FSH.WebApi.Application.Catalog and restart the program.

WIP :   code for nested parent - child 



Test Entities

TheParent.cs
------------
namespace FSH.WebApi.Domain.Catalog;
public class TheParent : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public TheParent(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public TheParent Update(string? name, string? description)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        return this;
    }
}

TheChild
--------
namespace FSH.WebApi.Domain.Catalog;
public class TheChild : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? ImagePath { get; private set; }
    public Guid TheParentId { get; private set; }
    public Guid BrandId { get; private set; }
    public virtual TheParent TheParent { get; private set; } = default!;
    public virtual Brand Brand { get; private set; }
    public TheChild(string name, string? description, string? imagePath, Guid theParentId, Guid brandId)
    {
        Name = name;
        Description = description;
        ImagePath = imagePath;
        TheParentId= theParentId;
        BrandId = brandId;
    }

    public TheChild Update(string? name, string? description, string? imagePath, Guid? theParentId, Guid? brandId)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (theParentId.HasValue && theParentId.Value != Guid.Empty && !TheParentId.Equals(theParentId.Value)) TheParentId = theParentId.Value;
        if (brandId.HasValue && brandId.Value != Guid.Empty && !BrandId.Equals(brandId.Value)) BrandId = brandId.Value;
        if (imagePath is not null && ImagePath?.Equals(imagePath) is not true) ImagePath = imagePath;
        return this;
    }

    public TheChild ClearImagePath()
    {
        ImagePath = string.Empty;
        return this;
    }
}

Applicationdbcontext.cs
-----------------------
using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Common.Events;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Domain.Catalog;
using FSH.WebApi.Infrastructure.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.WebApi.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
    public ApplicationDbContext(ITenantInfo currentTenant, DbContextOptions options, ICurrentUser currentUser, ISerializerService serializer, IOptions<DatabaseSettings> dbSettings, IEventPublisher events)
        : base(currentTenant, options, currentUser, serializer, dbSettings, events)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<TheChild> TheChilds => Set<TheChild>();
    public DbSet<TheParent> TheParents => Set<TheParent>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaNames.Catalog);
    }
}

entityrelated words 
-------------------
<&Entity&>          : the entity
<&EntityToLower&>   : lowercaes entity (could be replaced with <&Entity&>.ToLower()
<&theusings&>       : generated usings path 
<&EventsPath&>      : path to the Event  (using <&EventsPath&>)
<&StringNameSpace&> : namespace
<&PropertyLines&>   : the properties from the entity in & object
<&RelationalLines&> : the relation in the entity in object
<&DetailLines&>     : the detail lines as used in the DTO
<&ReadRepository&>  : the repository object
<&ValidatorName&>  : validator name as in the validator code
<&ValidatorNameToLower&>: if the validatorname is needed in lowercase.
<&ChildEntity&>     : name of the child (deletion check for parent)
<&ChildEntityPLural&>: plural
<&Parent&>          : Parent entity
<&ParentToLower&>   : Parent to lower





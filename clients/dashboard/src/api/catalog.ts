import { apiFetch } from "@/lib/api-client";

export type PagedResponse<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
};

export type BrandDto = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  logoUrl?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  deletedOnUtc?: string | null;
  deletedBy?: string | null;
};

export type SearchBrandsParams = {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateBrandInput = {
  name: string;
  description?: string | null;
  logoUrl?: string | null;
};

export type UpdateBrandInput = {
  brandId: string;
  name: string;
  description?: string | null;
  logoUrl?: string | null;
};

export function searchBrands(params: SearchBrandsParams = {}): Promise<PagedResponse<BrandDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<BrandDto>>(`/api/v1/catalog/brands?${query.toString()}`);
}

export function getBrandById(id: string): Promise<BrandDto> {
  return apiFetch<BrandDto>(`/api/v1/catalog/brands/${encodeURIComponent(id)}`);
}

export async function createBrand(input: CreateBrandInput): Promise<string> {
  return apiFetch<string>("/api/v1/catalog/brands", {
    method: "POST",
    body: JSON.stringify({
      name: input.name,
      description: input.description ?? null,
      logoUrl: input.logoUrl ?? null,
    }),
  });
}

export async function updateBrand(input: UpdateBrandInput): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/brands/${encodeURIComponent(input.brandId)}`, {
    method: "PUT",
    body: JSON.stringify({
      brandId: input.brandId,
      name: input.name,
      description: input.description ?? null,
      logoUrl: input.logoUrl ?? null,
    }),
  });
}

export async function deleteBrand(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/brands/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Categories ────────────────────────────────────────────────────────

export type CategoryDto = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  parentCategoryId?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  deletedOnUtc?: string | null;
  deletedBy?: string | null;
};

export type CategoryTreeNodeDto = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  children: CategoryTreeNodeDto[];
};

export type SearchCategoriesParams = {
  search?: string;
  parentCategoryId?: string | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateCategoryInput = {
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
};

export type UpdateCategoryInput = {
  categoryId: string;
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
};

export function searchCategories(
  params: SearchCategoriesParams = {},
): Promise<PagedResponse<CategoryDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.parentCategoryId) query.set("parentCategoryId", params.parentCategoryId);
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 50));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<CategoryDto>>(
    `/api/v1/catalog/categories?${query.toString()}`,
  );
}

export function getCategoryTree(): Promise<CategoryTreeNodeDto[]> {
  return apiFetch<CategoryTreeNodeDto[]>("/api/v1/catalog/categories/tree");
}

export function getCategoryById(id: string): Promise<CategoryDto> {
  return apiFetch<CategoryDto>(`/api/v1/catalog/categories/${encodeURIComponent(id)}`);
}

export async function createCategory(input: CreateCategoryInput): Promise<string> {
  return apiFetch<string>("/api/v1/catalog/categories", {
    method: "POST",
    body: JSON.stringify({
      name: input.name,
      description: input.description ?? null,
      parentCategoryId: input.parentCategoryId ?? null,
    }),
  });
}

export async function updateCategory(input: UpdateCategoryInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/categories/${encodeURIComponent(input.categoryId)}`,
    {
      method: "PUT",
      body: JSON.stringify({
        categoryId: input.categoryId,
        name: input.name,
        description: input.description ?? null,
        parentCategoryId: input.parentCategoryId ?? null,
      }),
    },
  );
}

export async function deleteCategory(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/categories/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Products ──────────────────────────────────────────────────────────

export type MoneyDto = {
  amount: number;
  currency: string;
};

export type ProductImageDto = {
  id: string;
  fileAssetId?: string | null;
  url: string;
  isThumbnail: boolean;
  sortOrder: number;
  createdAtUtc: string;
};

export type ProductDto = {
  id: string;
  sku: string;
  name: string;
  slug: string;
  description?: string | null;
  brandId: string;
  categoryId: string;
  price: MoneyDto;
  stock: number;
  isActive: boolean;
  /** Convenience: the URL of the thumbnail image (if any). Derived server-side from images[]. */
  thumbnailUrl?: string | null;
  images: ProductImageDto[];
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  deletedOnUtc?: string | null;
  deletedBy?: string | null;
};

export type SearchProductsParams = {
  search?: string;
  brandId?: string | null;
  categoryId?: string | null;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateProductInput = {
  sku: string;
  name: string;
  description?: string | null;
  brandId: string;
  categoryId: string;
  priceAmount: number;
  priceCurrency: string;
  stock: number;
};

export type UpdateProductInput = {
  productId: string;
  name: string;
  description?: string | null;
  brandId: string;
  categoryId: string;
  isActive: boolean;
};

export type ChangeProductPriceInput = {
  productId: string;
  amount: number;
  currency: string;
};

export type AdjustProductStockInput = {
  productId: string;
  delta: number;
};

export function searchProducts(
  params: SearchProductsParams = {},
): Promise<PagedResponse<ProductDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.brandId) query.set("brandId", params.brandId);
  if (params.categoryId) query.set("categoryId", params.categoryId);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<ProductDto>>(
    `/api/v1/catalog/products?${query.toString()}`,
  );
}

export function getProductById(id: string): Promise<ProductDto> {
  return apiFetch<ProductDto>(`/api/v1/catalog/products/${encodeURIComponent(id)}`);
}

export async function createProduct(input: CreateProductInput): Promise<string> {
  return apiFetch<string>("/api/v1/catalog/products", {
    method: "POST",
    body: JSON.stringify({
      sku: input.sku,
      name: input.name,
      description: input.description ?? null,
      brandId: input.brandId,
      categoryId: input.categoryId,
      priceAmount: input.priceAmount,
      priceCurrency: input.priceCurrency,
      stock: input.stock,
    }),
  });
}

export async function updateProduct(input: UpdateProductInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(input.productId)}`,
    {
      method: "PUT",
      body: JSON.stringify({
        productId: input.productId,
        name: input.name,
        description: input.description ?? null,
        brandId: input.brandId,
        categoryId: input.categoryId,
        isActive: input.isActive,
      }),
    },
  );
}

// ─── Product images ───────────────────────────────────────────────────

export function addProductImage(
  productId: string,
  input: { fileAssetId?: string | null; url: string },
): Promise<ProductImageDto> {
  return apiFetch<ProductImageDto>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/images`,
    {
      method: "POST",
      body: JSON.stringify({
        fileAssetId: input.fileAssetId ?? null,
        url: input.url,
      }),
    },
  );
}

export async function removeProductImage(productId: string, imageId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/images/${encodeURIComponent(imageId)}`,
    { method: "DELETE" },
  );
}

export async function setProductThumbnail(productId: string, imageId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/images/${encodeURIComponent(imageId)}/thumbnail`,
    { method: "PUT" },
  );
}

export async function changeProductPrice(input: ChangeProductPriceInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(input.productId)}/price`,
    {
      method: "PATCH",
      body: JSON.stringify({
        productId: input.productId,
        amount: input.amount,
        currency: input.currency,
      }),
    },
  );
}

export async function adjustProductStock(
  input: AdjustProductStockInput,
): Promise<{ stock: number }> {
  return apiFetch<{ stock: number }>(
    `/api/v1/catalog/products/${encodeURIComponent(input.productId)}/stock`,
    {
      method: "PATCH",
      body: JSON.stringify({
        productId: input.productId,
        delta: input.delta,
      }),
    },
  );
}

export async function deleteProduct(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/products/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Trash + Restore ──────────────────────────────────────────────────

export function listTrashedBrands(
  pageNumber = 1,
  pageSize = 20,
): Promise<PagedResponse<BrandDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<BrandDto>>(`/api/v1/catalog/brands/trash?${q.toString()}`);
}

export function restoreBrand(id: string): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/brands/${encodeURIComponent(id)}/restore`, {
    method: "POST",
  });
}

export function listTrashedCategories(
  pageNumber = 1,
  pageSize = 20,
): Promise<PagedResponse<CategoryDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<CategoryDto>>(
    `/api/v1/catalog/categories/trash?${q.toString()}`,
  );
}

export function restoreCategory(id: string): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/categories/${encodeURIComponent(id)}/restore`, {
    method: "POST",
  });
}

export function listTrashedProducts(
  pageNumber = 1,
  pageSize = 20,
): Promise<PagedResponse<ProductDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<ProductDto>>(
    `/api/v1/catalog/products/trash?${q.toString()}`,
  );
}

export function restoreProduct(id: string): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/products/${encodeURIComponent(id)}/restore`, {
    method: "POST",
  });
}

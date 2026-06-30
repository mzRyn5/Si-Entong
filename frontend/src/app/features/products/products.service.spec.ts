import { TestBed } from '@angular/core/testing';
import { provideHttpClient, HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ProductsService, ProductQueryParams } from './products.service';

describe('ProductsService', () => {
  let service: ProductsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ProductsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAll()', () => {
    it('should call GET /api/v1/products with query params', () => {
      const params: ProductQueryParams = { page: 1, pageSize: 20, search: 'Indomie' };

      service.getAll(params).subscribe();

      const req = httpMock.expectOne('/api/v1/products?page=1&pageSize=20&search=Indomie');
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, data: [], meta: { page: 1, pageSize: 20, totalItems: 0, totalPages: 1 } });
    });

    it('should omit undefined params from request', () => {
      const params: ProductQueryParams = { page: 1, pageSize: 20 };

      service.getAll(params).subscribe();

      const req = httpMock.expectOne('/api/v1/products?page=1&pageSize=20');
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, data: [], meta: { page: 1, pageSize: 20, totalItems: 0, totalPages: 1 } });
    });
  });

  describe('getById()', () => {
    it('should call GET /api/v1/products/{id}', () => {
      const productId = 'prod-123';

      service.getById(productId).subscribe();

      const req = httpMock.expectOne(`/api/v1/products/${productId}`);
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, data: {} });
    });
  });

  describe('create()', () => {
    it('should call POST /api/v1/products with body', () => {
      const payload = {
        name: 'Test Product',
        sku: 'SKU-001',
        unitId: 'unit-1',
        purchasePrice: 5000,
        sellingPrice: 7500,
        currentStock: 10,
        lowStockThreshold: 5,
        isActive: true
      };

      service.create(payload).subscribe();

      const req = httpMock.expectOne('/api/v1/products');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(payload);
      req.flush({ success: true, data: {} });
    });
  });

  describe('update()', () => {
    it('should call PUT /api/v1/products/{id}', () => {
      const id = 'prod-123';
      const payload = { name: 'Updated Product', sellingPrice: 8000 };

      service.update(id, payload).subscribe();

      const req = httpMock.expectOne(`/api/v1/products/${id}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(payload);
      req.flush({ success: true, data: {} });
    });
  });

  describe('delete()', () => {
    it('should call DELETE /api/v1/products/{id}', () => {
      const id = 'prod-123';

      service.delete(id).subscribe();

      const req = httpMock.expectOne(`/api/v1/products/${id}`);
      expect(req.request.method).toBe('DELETE');
      req.flush({ success: true, data: null });
    });
  });

  describe('getLowStock()', () => {
    it('should call GET /api/v1/products/low-stock with params', () => {
      service.getLowStock({ page: 1, pageSize: 10 }).subscribe();

      const req = httpMock.expectOne('/api/v1/products/low-stock?page=1&pageSize=10');
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, data: [], meta: { page: 1, pageSize: 10, totalItems: 0, totalPages: 1 } });
    });
  });
});

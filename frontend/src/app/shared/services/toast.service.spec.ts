import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';
describe('ToastService', () => { it('creates', () => { TestBed.configureTestingModule({}); expect(TestBed.inject(ToastService)).toBeTruthy(); }); });

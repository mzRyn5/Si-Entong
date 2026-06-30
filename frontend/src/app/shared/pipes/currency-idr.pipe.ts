import { Pipe, PipeTransform } from '@angular/core';
@Pipe({ name: 'currencyIdr', standalone: true })
export class CurrencyIdrPipe implements PipeTransform { transform(value: number | null | undefined): string { return new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(value ?? 0); } }

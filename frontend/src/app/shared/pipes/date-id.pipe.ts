import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'dateId',
  standalone: true
})
export class DateIdPipe implements PipeTransform {
  transform(value: string | Date | null | undefined, format: 'date' | 'datetime' | 'time' = 'date'): string {
    if (!value) return '-';
    const date = new Date(value);
    
    // Check for invalid date
    if (isNaN(date.getTime())) return '-';

    if (format === 'datetime') {
      return new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium', timeStyle: 'short' }).format(date);
    }
    if (format === 'time') {
      return new Intl.DateTimeFormat('id-ID', { timeStyle: 'short' }).format(date);
    }
    return new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium' }).format(date);
  }
}

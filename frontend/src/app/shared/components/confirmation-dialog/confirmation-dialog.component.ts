import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

export type DialogType = 'danger' | 'warning' | 'info' | 'success' | 'default';

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  templateUrl: './confirmation-dialog.component.html',
  styleUrl: './confirmation-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmationDialogComponent {
  @Input() type: DialogType = 'default';
  @Input() title = 'Konfirmasi';
  @Input() message = 'Apakah Anda yakin?';
  @Input() cancelLabel = 'Batal';
  @Input() confirmLabel = 'Konfirmasi';

  @Output() onCancel = new EventEmitter<void>();
  @Output() onConfirm = new EventEmitter<void>();

  handleCancel(): void {
    this.onCancel.emit();
  }

  handleConfirm(): void {
    this.onConfirm.emit();
  }
}

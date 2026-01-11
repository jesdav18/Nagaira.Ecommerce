import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProductRequestService } from '../../core/services/product-request.service';

@Component({
  selector: 'app-product-request',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './product-request.component.html',
  styleUrls: ['./product-request.component.css']
})
export class ProductRequestComponent {
  private productRequestService = inject(ProductRequestService);

  requestDescription = signal('');
  requestImageName = signal('');
  requestFile = signal<File | null>(null);
  requestName = signal('');
  requestPhone = signal('');
  requestEmail = signal('');
  requestCity = signal('');
  requestAddress = signal('');
  requestUrgency = signal('');
  requestLink = signal('');
  requestSubmitting = signal(false);
  requestSuccess = signal('');
  requestError = signal('');

  @ViewChild('requestFileInput') requestFileInput?: ElementRef<HTMLInputElement>;

  onRequestFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] || null;
    this.requestImageName.set(file ? file.name : '');
    this.requestFile.set(file);
  }

  submitRequest(): void {
    const description = this.requestDescription().trim();
    const name = this.requestName().trim();
    const phone = this.requestPhone().trim();
    const urgency = this.requestUrgency().trim();
    this.requestError.set('');
    this.requestSuccess.set('');

    if (!name || !phone || !description || !urgency) {
      this.requestError.set('Completa nombre, telefono, urgencia y descripcion.');
      return;
    }

    this.requestSubmitting.set(true);
    this.productRequestService.createRequest({
      description,
      name,
      phone,
      urgency,
      email: this.requestEmail().trim(),
      city: this.requestCity().trim(),
      address: this.requestAddress().trim(),
      link: this.requestLink().trim(),
      image: this.requestFile()
    }).subscribe({
      next: () => {
        this.requestSuccess.set('Encargo enviado. Te contactaremos pronto.');
        this.requestName.set('');
        this.requestDescription.set('');
        this.requestImageName.set('');
        this.requestFile.set(null);
        this.requestPhone.set('');
        this.requestEmail.set('');
        this.requestCity.set('');
        this.requestAddress.set('');
        this.requestUrgency.set('');
        this.requestLink.set('');
        if (this.requestFileInput) {
          this.requestFileInput.nativeElement.value = '';
        }
        this.requestSubmitting.set(false);
      },
      error: () => {
        this.requestError.set('No se pudo enviar el encargo. Intenta de nuevo.');
        this.requestSubmitting.set(false);
      }
    });
  }
}

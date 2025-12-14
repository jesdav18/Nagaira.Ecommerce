import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent {
  authService = inject(AuthService);
  private fb = inject(FormBuilder);


  passwordForm = this.fb.group({
    oldPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required]
  });

  loading = signal(false);
  message = signal<{ type: 'success' | 'error', text: string } | null>(null);

  onSubmitPassword() {
    if (this.passwordForm.invalid) return;

    const val = this.passwordForm.value;
    if (val.newPassword !== val.confirmPassword) {
      this.message.set({ type: 'error', text: 'Las contraseñas no coinciden' });
      return;
    }

    this.loading.set(true);
    this.message.set(null);

    this.authService.changePassword({ 
      oldPassword: val.oldPassword!, 
      newPassword: val.newPassword! 
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.message.set({ type: 'success', text: 'Contraseña actualizada correctamente' });
        this.passwordForm.reset();
      },
      error: (err) => {
        this.loading.set(false);
        this.message.set({ type: 'error', text: err.error?.message || 'Error al cambiar contraseña' });
      }
    });
  }
}

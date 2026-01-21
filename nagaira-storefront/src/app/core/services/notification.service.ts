import { Injectable } from '@angular/core';
import Swal from 'sweetalert2';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  success(message: string, title = 'Listo'): Promise<any> {
    return Swal.fire({ icon: 'success', title, text: message });
  }

  error(message: string, title = 'Error'): Promise<any> {
    return Swal.fire({ icon: 'error', title, text: message });
  }

  warning(message: string, title = 'Atencion'): Promise<any> {
    return Swal.fire({ icon: 'warning', title, text: message });
  }

  info(message: string, title = 'Informacion'): Promise<any> {
    return Swal.fire({ icon: 'info', title, text: message });
  }

  async confirm(message: string, title = 'Confirmar'): Promise<boolean> {
    const result = await Swal.fire({
      icon: 'question',
      title,
      text: message,
      showCancelButton: true,
      confirmButtonText: 'Si',
      cancelButtonText: 'No'
    });
    return result.isConfirmed;
  }
}

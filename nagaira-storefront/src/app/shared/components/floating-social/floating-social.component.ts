import { Component } from '@angular/core';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-floating-social',
  standalone: true,
  templateUrl: './floating-social.component.html',
  styleUrl: './floating-social.component.css'
})
export class FloatingSocialComponent {
  facebookUrl = environment.facebookUrl;
  whatsappUrl = `https://wa.me/${environment.whatsappCheckoutPhone}?text=${encodeURIComponent('Hola, quiero mas informacion sobre Nagaira.')}`;
}

import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => {
    console.error('Error al inicializar la aplicación:', err);
    document.body.innerHTML = `
      <div style="padding: 20px; text-align: center; font-family: Arial, sans-serif;">
        <h1>Error al cargar la aplicación</h1>
        <p>Por favor, recarga la página o contacta al soporte.</p>
        <button id="reload-app" style="padding: 10px 20px; margin-top: 10px;">
          Recargar Página
        </button>
      </div>
    `;
    const reloadButton = document.getElementById('reload-app');
    if (reloadButton) {
      reloadButton.addEventListener('click', () => location.reload());
    }
  });



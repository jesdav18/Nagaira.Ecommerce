# Apache RewriteMap para 301 de slugs

Este documento deja los pasos para habilitar redirecciones 301 por cambio de slug usando el endpoint:

`GET /api/seo/redirect/{type}/{slug}`

Tipos aceptados: `p` (product) y `c` (category).

## 1) Crear script de resolucion

Crear el archivo `/usr/local/bin/resolve-slug.sh`:

```bash
#!/usr/bin/env bash
while read line; do
  type=$(echo "$line" | cut -d'|' -f1)
  slug=$(echo "$line" | cut -d'|' -f2)
  result=$(curl -s "http://localhost:5011/api/seo/redirect/${type}/${slug}" -o /dev/null -w "%{redirect_url}")
  if [ -n "$result" ]; then
    echo "$result"
  else
    echo ""
  fi
done
```

Dar permisos:

```bash
sudo chmod +x /usr/local/bin/resolve-slug.sh
```

## 2) Activar mod_rewrite

```bash
sudo a2enmod rewrite
sudo systemctl restart apache2
```

## 3) Agregar RewriteMap en el VirtualHost

En el vhost de `nagaira.com` (puerto 80 y/o 443):

```apache
RewriteEngine On

RewriteMap slugmap prg:/usr/local/bin/resolve-slug.sh

# Redirigir solo si el resolver devuelve URL
RewriteCond ${slugmap:$1|$2} !=""
RewriteRule ^/ecommerce/(p|c)/(.+)$ ${slugmap:$1|$2} [R=301,L]
```

Reiniciar:

```bash
sudo systemctl reload apache2
```

## 4) Probar

```bash
curl -I https://nagaira.com/ecommerce/p/slug-antiguo
```

Debe responder con `301` y `Location` al slug nuevo.

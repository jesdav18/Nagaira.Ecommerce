import { writeFile } from 'node:fs/promises';

const apiBase = process.env.API_BASE_URL || 'http://localhost:5098/api';

async function fetchJson(path) {
  const response = await fetch(`${apiBase}${path}`);
  if (!response.ok) {
    throw new Error(`Request failed ${response.status} for ${path}`);
  }
  return response.json();
}

async function main() {
  const [products, categories] = await Promise.all([
    fetchJson('/products'),
    fetchJson('/categories')
  ]);

  const routes = new Set(['/']);

  for (const product of products) {
    if (product.slug) {
      routes.add(`/p/${product.slug}`);
    }
  }

  for (const category of categories) {
    if (category.slug) {
      routes.add(`/c/${category.slug}`);
    }
  }

  const output = Array.from(routes).sort().join('\n');
  await writeFile('prerender-routes.txt', output, 'utf-8');
  console.log(`Generated ${routes.size} prerender routes.`);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

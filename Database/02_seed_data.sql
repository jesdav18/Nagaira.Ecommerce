INSERT INTO categories (id, name, description, slug, parent_category_id, is_active)
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Electrónica', 'Productos electrónicos', 'electronica', NULL, TRUE),
    ('22222222-2222-2222-2222-222222222222', 'Ropa', 'Ropa y accesorios', 'ropa', NULL, TRUE),
    ('33333333-3333-3333-3333-333333333333', 'Hogar', 'Artículos para el hogar', 'hogar', NULL, TRUE),
    ('44444444-4444-4444-4444-444444444444', 'Smartphones', 'Teléfonos inteligentes', 'smartphones', '11111111-1111-1111-1111-111111111111', TRUE),
    ('55555555-5555-5555-5555-555555555555', 'Laptops', 'Computadoras portátiles', 'laptops', '11111111-1111-1111-1111-111111111111', TRUE);

INSERT INTO products (id, name, description, sku, price, discount_price, stock, category_id, is_active)
VALUES 
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'iPhone 15 Pro', 'Smartphone Apple última generación', 'IPH15PRO', 1299.99, 1199.99, 50, '44444444-4444-4444-4444-444444444444', TRUE),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Samsung Galaxy S24', 'Smartphone Samsung flagship', 'SAMS24', 999.99, NULL, 75, '44444444-4444-4444-4444-444444444444', TRUE),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'MacBook Pro 16"', 'Laptop profesional Apple', 'MBP16', 2499.99, 2299.99, 30, '55555555-5555-5555-5555-555555555555', TRUE),
    ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'Dell XPS 15', 'Laptop premium Dell', 'DELLXPS15', 1799.99, NULL, 40, '55555555-5555-5555-5555-555555555555', TRUE);

INSERT INTO product_images (id, product_id, image_url, alt_text, display_order, is_primary)
VALUES 
    (uuid_generate_v4(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '/images/iphone15pro-1.jpg', 'iPhone 15 Pro frontal', 1, TRUE),
    (uuid_generate_v4(), 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '/images/galaxys24-1.jpg', 'Samsung Galaxy S24', 1, TRUE),
    (uuid_generate_v4(), 'cccccccc-cccc-cccc-cccc-cccccccccccc', '/images/macbookpro-1.jpg', 'MacBook Pro 16 pulgadas', 1, TRUE),
    (uuid_generate_v4(), 'dddddddd-dddd-dddd-dddd-dddddddddddd', '/images/dellxps15-1.jpg', 'Dell XPS 15', 1, TRUE);

INSERT INTO users (id, email, password_hash, first_name, last_name, phone_number, role, is_active)
VALUES 
    ('99999999-9999-9999-9999-999999999999', 'admin@nagaira.com', '$2a$11$LQKvFQKqPqZ8xKxZxKxZxOqKqKqKqKqKqKqKqKqKqKqKqKqKqKqKq', 'Admin', 'Nagaira', '+1234567890', 3, TRUE);

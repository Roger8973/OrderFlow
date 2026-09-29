CREATE TABLE customers (
    id uuid PRIMARY KEY,
    name varchar(200) NOT NULL,
    email varchar(254) NOT NULL,
    phone varchar(30) NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_customers_email ON customers (lower(email));

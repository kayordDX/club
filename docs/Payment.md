# Payment Details

## PayFast (Custom Integration — Form POST Flow)

### Configuration

| Field               | Value                                                 |
| ------------------- | ----------------------------------------------------- |
| `BaseUrl` (Live)    | `https://www.payfast.co.za/eng/process`               |
| `BaseUrl` (Sandbox) | `https://sandbox.payfast.co.za/eng/process`           |
| `MerchantId`        | From PayFast dashboard                                |
| `MerchantKey`       | From PayFast dashboard                                |
| `Passphrase`        | Set in PayFast dashboard → Settings → Salt Passphrase |

### Flow

1. Backend builds form fields in **documentation order** (not alphabetical)
2. Calculates MD5 signature with passphrase as salt
3. Returns `FormActionUrl` + `FormFields` to the frontend
4. Frontend renders a hidden `<form>` with `method="post"` and auto-submits it
5. PayFast processes payment and redirects user to `return_url` (routed through the API's
   `GET /payment/result/payfast` — the signed return fields are verified there, the booking is
   marked paid/confirmed, then the shopper is redirected to the frontend `/payment/success` page)
6. PayFast sends ITN (Instant Transaction Notification) to `notify_url`
7. Backend verifies (ITN POST):
   - Signature match (MD5)
   - Valid PayFast IP
   - Payment data (amount matches)
   - Server-to-server validation via `https://www.payfast.co.za/eng/query/validate`

> The browser return (GET) is verified by signature only — the server-to-server validation step
> applies to the ITN POST. This keeps the booking status update working in local POCs where
> PayFast's ITN cannot reach a `localhost` notify_url; the ITN remains the authoritative capture
> on production deployments.

### Signature Rules (IMPORTANT)

- Fields must be in **documentation order** — NOT alphabetical
- URL encoding must use **uppercase hex** (e.g. `%3A` not `%3a`) and spaces as `+`
- Passphrase is appended as `&passphrase=...` at the end of the parameter string

### Merchant Credentials (Sandbox Testing)

```
Merchant ID: 10000100
Merchant Key: 46f0cd694581a
Passphrase: jt7NOE43FZPn
```

Buyer: `sbtu01@payfast.io` / `clientpass`

### Resources

- Custom Integration: https://developers.payfast.co.za/docs
- API Reference: https://developers.payfast.co.za/api
- ITN Validation: https://www.payfast.co.za/eng/query/validate

## Peach Payments (API — Onsite/Redirect Flow)

Peach uses a server-to-server API call to create a checkout session, then returns a redirect URL for the frontend.

### Configuration

| Field      | Source                                                             |
| ---------- | ------------------------------------------------------------------ |
| `UserId`   | Peach dashboard                                                    |
| `Password` | Peach dashboard                                                    |
| `EntityId` | Peach dashboard                                                    |
| `BaseUrl`  | `https://oppwa.com/v1` (Live) / `https://test.oppwa.com/v1` (Test) |

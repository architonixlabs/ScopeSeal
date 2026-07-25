import { test, expect, APIRequestContext } from '@playwright/test';

const apiBaseUrl = process.env['SCOPESEAL_API_URL'] ?? 'http://localhost:5080';

const minimalPdf = Buffer.from(
  '%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\ntrailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n149\n%%EOF',
);

async function registerAndLogin(request: APIRequestContext, suffix: string) {
  const email = `e2e-${suffix}@example.com`;
  const password = 'SecurePass1!';

  const register = await request.post(`${apiBaseUrl}/api/v1/auth/register`, {
    data: {
      email,
      password,
      displayName: 'E2E User',
      tenantName: `E2E Tenant ${suffix}`,
      confirmedAge18OrAbove: true,
    },
  });
  expect(register.status()).toBe(201);

  const login = await request.post(`${apiBaseUrl}/api/v1/auth/login`, {
    data: { email, password },
  });
  expect(login.status()).toBe(204);

  const me = await request.get(`${apiBaseUrl}/api/v1/auth/me`);
  expect(me.status()).toBe(200);
  const meBody = await me.json();
  const tenantPublicId = meBody.tenant.publicId as string;
  return { request, tenantPublicId, email };
}

test.describe('API smoke flows', () => {
  test.skip(!process.env['SCOPESEAL_API_URL'] && !process.env['CI'], 'Requires running API (set SCOPESEAL_API_URL)');

  test('registration and auth/me succeed', async ({ request }) => {
    const suffix = Date.now().toString(36);
    const { email } = await registerAndLogin(request, suffix);
    const me = await request.get(`${apiBaseUrl}/api/v1/auth/me`);
    const body = await me.json();
    expect(body.email).toBe(email);
  });

  test('workspace create and list succeed', async ({ request }) => {
    const suffix = Date.now().toString(36);
    const { tenantPublicId } = await registerAndLogin(request, suffix);

    const create = await request.post(`${apiBaseUrl}/api/v1/tenants/${tenantPublicId}/workspaces`, {
      data: {
        name: 'E2E workspace',
        description: 'Playwright smoke workspace',
        type: 'InteriorDesign',
      },
    });
    expect(create.status()).toBe(201);
    const workspace = await create.json();
    expect(workspace.publicId).toBeTruthy();

    const list = await request.get(`${apiBaseUrl}/api/v1/tenants/${tenantPublicId}/workspaces`);
    expect(list.status()).toBe(200);
    const workspaces = await list.json();
    expect(workspaces.length).toBeGreaterThanOrEqual(1);
  });

  test('upload session stub completes for minimal PDF', async ({ request }) => {
    const suffix = Date.now().toString(36);
    const { tenantPublicId } = await registerAndLogin(request, suffix);

    const workspaceResponse = await request.post(`${apiBaseUrl}/api/v1/tenants/${tenantPublicId}/workspaces`, {
      data: { name: 'Upload workspace', type: 'General' },
    });
    const workspace = await workspaceResponse.json();
    const workspacePublicId = workspace.publicId as string;

    const sessionResponse = await request.post(
      `${apiBaseUrl}/api/v1/tenants/${tenantPublicId}/workspaces/${workspacePublicId}/upload-sessions`,
      {
        data: {
          originalFileName: 'scope.pdf',
          declaredContentType: 'application/pdf',
          expectedBytes: minimalPdf.length,
        },
      },
    );
    expect(sessionResponse.status()).toBe(201);
    const session = await sessionResponse.json();
    const sessionPublicId = session.publicId as string;

    const upload = await request.put(
      `${apiBaseUrl}/api/v1/tenants/${tenantPublicId}/workspaces/${workspacePublicId}/upload-sessions/${sessionPublicId}/content`,
      {
        multipart: {
          file: {
            name: 'scope.pdf',
            mimeType: 'application/pdf',
            buffer: minimalPdf,
          },
        },
      },
    );
    expect(upload.status()).toBe(200);

    const complete = await request.post(
      `${apiBaseUrl}/api/v1/tenants/${tenantPublicId}/workspaces/${workspacePublicId}/upload-sessions/${sessionPublicId}/complete`,
    );
    expect(complete.status()).toBe(200);
    const completeBody = await complete.json();
    expect(completeBody.document.publicId).toBeTruthy();
  });
});

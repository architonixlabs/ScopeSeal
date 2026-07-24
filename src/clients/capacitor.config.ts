import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'in.architonix.scopeseal',
  appName: 'ScopeSeal',
  webDir: 'dist/product-app/browser',
  server: {
    androidScheme: 'https',
  },
};

export default config;

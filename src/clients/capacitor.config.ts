import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'in.architonix.scopeseal',
  appName: 'ScopeSeal',
  webDir: 'dist/product-app/browser',
  server: {
    androidScheme: 'https',
  },
  android: {
    allowMixedContent: false,
  },
  ios: {
    contentInset: 'automatic',
  },
  plugins: {
    SplashScreen: {
      launchAutoHide: true,
      backgroundColor: '#ffffff',
    },
    StatusBar: {
      style: 'LIGHT',
    },
  },
};

export default config;

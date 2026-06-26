// Default (production) environment. Overridden by environment.development.ts during `ng serve`
// via the fileReplacements in angular.json. Point apiBaseUrl at the API Gateway.
export const environment = {
  production: true,
  apiBaseUrl: '/api-gateway', // replace with the deployed gateway origin
};

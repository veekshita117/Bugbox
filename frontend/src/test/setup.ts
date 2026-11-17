import '@testing-library/jest-dom';

// ResizeObserver mock for Recharts and layout tests
global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

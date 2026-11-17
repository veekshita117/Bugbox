import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './context/AuthContext';
import { ToastProvider } from './context/ToastContext';
import { ProtectedRoute } from './components/layout/ProtectedRoute';
import { AppLayout } from './components/layout/AppLayout';
import { LoginPage } from './features/auth/LoginPage';
import { RegisterPage } from './features/auth/RegisterPage';
import { ProjectsPage } from './features/projects/ProjectsPage';
import { ProjectLayout } from './features/projects/ProjectLayout';
import { BoardTab } from './features/board/BoardTab';
import { BacklogTab } from './features/backlog/BacklogTab';
import { SprintsTab } from './features/sprints/SprintsTab';
import { SettingsTab } from './features/settings/SettingsTab';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

export const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <AuthProvider>
          <BrowserRouter>
            <Routes>
              {/* Public Routes */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />

              {/* Protected Routes */}
              <Route element={<ProtectedRoute />}>
                <Route element={<AppLayout />}>
                  <Route path="/projects" element={<ProjectsPage />} />

                  {/* Project Workspace Tabs */}
                  <Route path="/projects/:projectId" element={<ProjectLayout />}>
                    <Route index element={<Navigate to="board" replace />} />
                    <Route path="board" element={<BoardTab />} />
                    <Route path="backlog" element={<BacklogTab />} />
                    <Route path="sprints" element={<SprintsTab />} />
                    <Route path="settings" element={<SettingsTab />} />
                  </Route>

                  <Route path="/" element={<Navigate to="/projects" replace />} />
                </Route>
              </Route>

              {/* Fallback */}
              <Route path="*" element={<Navigate to="/projects" replace />} />
            </Routes>
          </BrowserRouter>
        </AuthProvider>
      </ToastProvider>
    </QueryClientProvider>
  );
};

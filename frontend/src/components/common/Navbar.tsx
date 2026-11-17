import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Bug, FolderKanban, LogOut } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { Avatar } from './Avatar';

export const Navbar: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className="sticky top-0 z-40 bg-white/80 backdrop-blur-md border-b border-slate-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        <div className="flex items-center gap-8">
          <Link to="/projects" className="flex items-center gap-2.5 group">
            <div className="w-9 h-9 rounded-xl bg-brand-600 flex items-center justify-center text-white shadow-md shadow-brand-500/20 group-hover:bg-brand-700 transition">
              <Bug className="w-5 h-5" />
            </div>
            <span className="font-bold text-lg tracking-tight text-slate-900">
              Bug<span className="text-brand-600">box</span>
            </span>
          </Link>

          <nav className="flex items-center gap-1">
            <Link
              to="/projects"
              className="flex items-center gap-2 px-3 py-2 text-sm font-medium text-slate-600 hover:text-slate-900 rounded-lg hover:bg-slate-100 transition"
            >
              <FolderKanban className="w-4 h-4 text-slate-500" />
              Projects
            </Link>
          </nav>
        </div>

        {user && (
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-3 pl-3 border-l border-slate-200">
              <Avatar name={user.name} size="md" />
              <div className="hidden sm:block text-left">
                <div className="text-sm font-semibold text-slate-800 leading-tight">{user.name}</div>
                <div className="text-xs text-slate-400">{user.email}</div>
              </div>
            </div>

            <button
              onClick={handleLogout}
              title="Sign out"
              className="p-2 text-slate-400 hover:text-rose-600 rounded-lg hover:bg-rose-50 transition"
            >
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        )}
      </div>
    </header>
  );
};

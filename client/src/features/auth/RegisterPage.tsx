import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../../components/ui/Button';
import { useAuth } from '../../contexts/useAuth';

export function RegisterPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [formState, setFormState] = useState({ email: '', password: '', displayName: '', companyName: '' });
  const [error, setError] = useState('');

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');
    try {
      const normalizedEmail = formState.email.trim().toLowerCase();
      const response = await fetch('http://localhost:5206/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...formState, email: normalizedEmail }),
      });
      const payload = await response.json();
      if (!response.ok) throw new Error(payload.message || 'Unable to register');
      login(payload.token, { userName: payload.userName, role: payload.role, tenantId: payload.tenantId, tenantName: payload.tenantName });
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to register');
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-[radial-gradient(circle_at_top_left,_rgba(120,119,198,0.2),_transparent_25%),linear-gradient(135deg,_#020617,_#0f172a_55%,_#111827)] p-6">
      <div className="w-full max-w-lg rounded-[32px] border border-white/10 bg-slate-900/70 p-8 shadow-2xl shadow-black/20 backdrop-blur-xl">
        <div className="mb-6 space-y-2">
          <p className="text-sm uppercase tracking-[0.3em] text-violet-300">InvenFlow AI</p>
          <h1 className="text-2xl font-semibold text-white">Create your tenant workspace</h1>
          <p className="text-sm text-slate-400">Launch a secure company profile with role-based controls from day one.</p>
        </div>
        <form className="space-y-4" onSubmit={handleSubmit}>
          <div className="grid gap-4 md:grid-cols-2">
            <label className="block text-sm text-slate-300">
              <span className="mb-2 block">Full Name</span>
              <input value={formState.displayName} onChange={(event) => setFormState((prev) => ({ ...prev, displayName: event.target.value }))} className="w-full rounded-2xl border border-white/10 bg-slate-950/60 px-3 py-3 text-white outline-none" />
            </label>
            <label className="block text-sm text-slate-300">
              <span className="mb-2 block">Company Name</span>
              <input value={formState.companyName} onChange={(event) => setFormState((prev) => ({ ...prev, companyName: event.target.value }))} className="w-full rounded-2xl border border-white/10 bg-slate-950/60 px-3 py-3 text-white outline-none" />
            </label>
          </div>
          <label className="block text-sm text-slate-300">
            <span className="mb-2 block">Email</span>
            <input type="email" value={formState.email} onChange={(event) => setFormState((prev) => ({ ...prev, email: event.target.value }))} className="w-full rounded-2xl border border-white/10 bg-slate-950/60 px-3 py-3 text-white outline-none" />
          </label>
          <label className="block text-sm text-slate-300">
            <span className="mb-2 block">Password</span>
            <input type="password" value={formState.password} onChange={(event) => setFormState((prev) => ({ ...prev, password: event.target.value }))} className="w-full rounded-2xl border border-white/10 bg-slate-950/60 px-3 py-3 text-white outline-none" />
          </label>
          {error ? <div className="rounded-2xl border border-rose-500/20 bg-rose-500/10 px-3 py-2 text-sm text-rose-300">{error}</div> : null}
          <Button type="submit" className="w-full">Create workspace</Button>
        </form>
      </div>
    </div>
  );
}

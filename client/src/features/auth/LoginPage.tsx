import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../../components/ui/Button';
import { useAuth } from '../../contexts/useAuth';

export function LoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');
    try {
      const normalizedEmail = email.trim().toLowerCase();
      const response = await fetch('http://localhost:5206/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: normalizedEmail, password }),
      });
      const payload = await response.json();
      if (!response.ok) throw new Error(payload.message || 'Unable to sign in');
      login(payload.token, { userName: payload.userName, role: payload.role, tenantId: payload.tenantId, tenantName: payload.tenantName });
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to sign in');
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-[radial-gradient(circle_at_top_left,_rgba(120,119,198,0.2),_transparent_25%),linear-gradient(135deg,_#020617,_#0f172a_55%,_#111827)] p-6">
      <div className="w-full max-w-md rounded-[32px] border border-white/10 bg-slate-900/70 p-8 shadow-2xl shadow-black/20 backdrop-blur-xl">
        <div className="mb-6 space-y-2">
          <p className="text-sm uppercase tracking-[0.3em] text-violet-300">InvenFlow AI</p>
          <h1 className="text-2xl font-semibold text-white">Welcome back</h1>
          <p className="text-sm text-slate-400">Secure multi-tenant workspace for modern operations teams.</p>
        </div>
        <form className="space-y-4" onSubmit={handleSubmit}>
          <label className="block text-sm text-slate-300">
            <span className="mb-2 block">Email</span>
            <input value={email} onChange={(event) => setEmail(event.target.value)} className="w-full rounded-2xl border border-white/10 bg-slate-950/60 px-3 py-3 text-white outline-none" />
          </label>
          <label className="block text-sm text-slate-300">
            <span className="mb-2 block">Password</span>
            <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} className="w-full rounded-2xl border border-white/10 bg-slate-950/60 px-3 py-3 text-white outline-none" />
          </label>
          {error ? <div className="rounded-2xl border border-rose-500/20 bg-rose-500/10 px-3 py-2 text-sm text-rose-300">{error}</div> : null}
          <Button type="submit" className="w-full">Sign in</Button>
        </form>
        <p className="mt-4 text-center text-sm text-slate-400">Need a workspace? <a className="text-violet-300" href="/register">Create one</a></p>
      </div>
    </div>
  );
}

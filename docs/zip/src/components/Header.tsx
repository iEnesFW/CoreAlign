import { Search, Sun, Bell, Command, Menu } from 'lucide-react';

export function Header() {
  return (
    <header className="h-16 border-b border-[#2a3143] bg-[#141824] flex items-center justify-between px-4 lg:px-8 flex-shrink-0 z-10 relative">
      <div className="flex items-center gap-4">
        <button className="md:hidden text-slate-400 hover:text-white">
          <Menu size={20} />
        </button>
        <div className="hidden sm:flex items-center gap-2 text-sm font-medium">
          <span className="text-slate-400 hover:text-slate-300 cursor-pointer transition-colors">Orders</span>
          <span className="text-slate-600">/</span>
          <span className="text-slate-200">New Order</span>
        </div>
      </div>

      <div className="flex flex-1 max-w-md mx-4 lg:mx-8">
        <div className="relative w-full group">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-indigo-400 transition-colors" size={16} />
          <input 
            type="text" 
            placeholder="Search..." 
            className="w-full bg-[#0f111a] border border-[#2a3143] rounded-md pl-9 pr-12 py-1.5 text-sm text-slate-200 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all placeholder-slate-500 hover:border-[#3b445e]"
          />
          <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center text-[10px] text-slate-500 font-mono bg-[#1a1f2e] px-1.5 py-0.5 rounded border border-[#2a3143]">
            <Command size={10} className="mr-0.5" /> K
          </div>
        </div>
      </div>

      <div className="flex items-center gap-3 lg:gap-5">
        <div className="hidden lg:flex items-center gap-2 px-3 py-1 bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 rounded-full text-xs font-medium">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
          <span>1 USD = 47.0098 TRY</span>
        </div>
        <button className="text-slate-400 hover:text-white transition-colors">
          <Sun size={18} />
        </button>
        <button className="text-slate-400 hover:text-white transition-colors relative">
          <Bell size={18} />
          <span className="absolute top-0 right-0 w-2 h-2 rounded-full bg-indigo-500 border border-[#141824]"></span>
        </button>
        <div className="flex items-center gap-3 pl-3 lg:pl-5 border-l border-[#2a3143] cursor-pointer group">
          <div className="w-8 h-8 rounded-full bg-indigo-600 flex items-center justify-center text-xs font-medium text-white shadow-sm group-hover:bg-indigo-500 transition-colors">
            EÇ
          </div>
          <div className="hidden sm:flex flex-col">
            <span className="text-sm font-medium text-slate-200 leading-tight group-hover:text-white transition-colors">Enes Çolak</span>
            <span className="text-[10px] text-slate-500 leading-tight mt-0.5">Demo Ticaret A.Ş.</span>
          </div>
        </div>
      </div>
    </header>
  );
}

import {
  LayoutDashboard,
  Users,
  FileText,
  ShoppingCart,
  FileSpreadsheet,
  RefreshCw,
  Undo2,
  FolderKanban,
  Truck,
  ShoppingBag,
} from 'lucide-react';

export function Sidebar() {
  return (
    <aside className="w-64 flex-shrink-0 bg-[#0f111a] border-r border-[#2a3143] h-screen overflow-y-auto hidden md:flex flex-col">
      <div className="h-16 px-6 flex items-center gap-3 border-b border-[#2a3143] flex-shrink-0">
        <div className="w-8 h-8 bg-indigo-600 rounded-md flex items-center justify-center font-bold text-white shadow-sm">
          CA
        </div>
        <span className="text-lg font-semibold text-slate-100 tracking-tight">CoreAlign</span>
      </div>

      <nav className="flex-1 px-3 py-6 space-y-8">
        <div>
          <div className="text-[11px] font-semibold text-slate-500 mb-3 px-3 uppercase tracking-wider">Overview</div>
          <a href="#" className="flex items-center gap-3 px-3 py-2 text-slate-300 hover:text-white hover:bg-[#1a1f2e] rounded-md transition-colors">
            <LayoutDashboard size={18} />
            <span className="text-sm font-medium">Dashboard</span>
          </a>
        </div>

        <div>
          <div className="text-[11px] font-semibold text-slate-500 mb-3 px-3 uppercase tracking-wider">Sales & CRM</div>
          <div className="space-y-0.5">
            <a href="#" className="flex items-center gap-3 px-3 py-2 text-slate-300 hover:text-white hover:bg-[#1a1f2e] rounded-md transition-colors">
              <Users size={18} />
              <span className="text-sm font-medium">Customers</span>
            </a>
            <a href="#" className="flex items-center gap-3 px-3 py-2 text-slate-300 hover:text-white hover:bg-[#1a1f2e] rounded-md transition-colors">
              <FileText size={18} />
              <span className="text-sm font-medium">Quotes</span>
            </a>
            <a href="#" className="flex items-center gap-3 px-3 py-2 text-indigo-400 bg-indigo-500/10 rounded-md transition-colors border border-indigo-500/20 shadow-sm">
              <ShoppingCart size={18} />
              <span className="text-sm font-medium">Orders</span>
            </a>
            <a href="#" className="flex items-center gap-3 px-3 py-2 text-slate-300 hover:text-white hover:bg-[#1a1f2e] rounded-md transition-colors">
              <FileSpreadsheet size={18} />
              <span className="text-sm font-medium">Invoices</span>
            </a>
            <a href="#" className="flex items-center gap-3 px-3 py-2 text-slate-300 hover:text-white hover:bg-[#1a1f2e] rounded-md transition-colors">
              <RefreshCw size={18} />
              <span className="text-sm font-medium">Recurring Invoices</span>
            </a>
            <a href="#" className="flex items-center gap-3 px-3 py-2 text-slate-300 hover:text-white hover:bg-[#1a1f2e] rounded-md transition-colors">
              <Undo2 size={18} />
              <span className="text-sm font-medium">Returns</span>
            </a>
          </div>
        </div>

        <div>
          <div className="text-[11px] font-semibold text-slate-500 mb-3 px-3 uppercase tracking-wider">Projects</div>
          <a href="#" className="flex items-center gap-3 px-3 py-2 text-slate-300 hover:text-white hover:bg-[#1a1f2e] rounded-md transition-colors">
            <FolderKanban size={18} />
            <span className="text-sm font-medium">Projects</span>
          </a>
        </div>
        
        <div>
          <div className="text-[11px] font-semibold text-slate-500 mb-3 px-3 uppercase tracking-wider">Purchasing</div>
          <div className="space-y-0.5">
            <a href="#" className="flex items-center gap-3 px-3 py-2 text-slate-300 hover:text-white hover:bg-[#1a1f2e] rounded-md transition-colors">
              <Truck size={18} />
              <span className="text-sm font-medium">Suppliers</span>
            </a>
            <a href="#" className="flex items-center gap-3 px-3 py-2 text-slate-300 hover:text-white hover:bg-[#1a1f2e] rounded-md transition-colors">
              <ShoppingBag size={18} />
              <span className="text-sm font-medium">Purchase Orders</span>
            </a>
          </div>
        </div>
      </nav>
      
      <div className="p-4 border-t border-[#2a3143] text-xs text-slate-500 flex justify-between items-center bg-[#0a0c12] flex-shrink-0">
        <span>© 2026 CoreAlign</span>
      </div>
    </aside>
  );
}

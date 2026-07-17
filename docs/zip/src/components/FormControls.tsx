import React from 'react';

export function FormInput({ label, type = 'text', placeholder, value, onChange, icon: Icon, required, className, defaultValue }: any) {
  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      <label className="text-xs font-medium text-slate-400 flex items-center justify-between">
        <span>{label} {required && <span className="text-red-400 ml-0.5">*</span>}</span>
      </label>
      <div className="relative group">
        <input
          type={type}
          placeholder={placeholder}
          value={value}
          defaultValue={defaultValue}
          onChange={onChange}
          className={`w-full bg-[#0f111a] border border-[#2a3143] rounded-md px-3 py-2 text-sm text-slate-200 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 hover:border-[#3b445e] transition-all placeholder-slate-600 ${Icon ? 'pl-9' : ''}`}
        />
        {Icon && (
          <div className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-indigo-400 transition-colors pointer-events-none">
            <Icon size={16} />
          </div>
        )}
      </div>
    </div>
  );
}

export function FormSelect({ label, options, value, onChange, required, className, action }: any) {
  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      <div className="flex items-center justify-between">
        <label className="text-xs font-medium text-slate-400">
          <span>{label} {required && <span className="text-red-400 ml-0.5">*</span>}</span>
        </label>
        {action && (
          <button type="button" className="text-[10px] uppercase tracking-wider font-semibold text-indigo-400 hover:text-indigo-300 flex items-center gap-1 transition-colors">
            {action.icon} {action.label}
          </button>
        )}
      </div>
      <div className="relative group">
        <select
          value={value}
          onChange={onChange}
          className="w-full bg-[#0f111a] border border-[#2a3143] rounded-md px-3 py-2 text-sm text-slate-200 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 hover:border-[#3b445e] transition-all appearance-none cursor-pointer"
        >
          <option value="" disabled className="text-slate-500">Select...</option>
          {options.map((opt: any) => (
            <option key={opt.value} value={opt.value}>{opt.label}</option>
          ))}
        </select>
        <div className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 pointer-events-none group-focus-within:text-indigo-400 transition-colors">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m6 9 6 6 6-6"/></svg>
        </div>
      </div>
    </div>
  );
}

export function FormTextarea({ label, placeholder, value, onChange, className }: any) {
  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      <label className="text-xs font-medium text-slate-400">{label}</label>
      <textarea
        placeholder={placeholder}
        value={value}
        onChange={onChange}
        rows={3}
        className="w-full bg-[#0f111a] border border-[#2a3143] rounded-md px-3 py-2 text-sm text-slate-200 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 hover:border-[#3b445e] transition-all placeholder-slate-600 resize-none"
      />
    </div>
  );
}

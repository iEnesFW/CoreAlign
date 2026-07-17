import { useState } from 'react';
import { Plus, Trash2, GripVertical, Settings2, ShoppingCart, ChevronDown, ChevronUp } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';

export function Step2() {
  const [items, setItems] = useState([
    { id: '1', product: '1', quantity: 1, price: 1500, discount: 0, vat: 20, expanded: false },
    { id: '2', product: '2', quantity: 3, price: 450, discount: 5, vat: 20, expanded: false },
  ]);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);

  const addItem = () => {
    setItems([...items, { id: Math.random().toString(), product: '', quantity: 1, price: 0, discount: 0, vat: 20, expanded: false }]);
  };

  const removeItem = (id: string) => {
    setItems(items.filter(i => i.id !== id));
    setConfirmDeleteId(null);
  };

  const toggleExpand = (id: string) => {
    setItems(items.map(i => i.id === id ? { ...i, expanded: !i.expanded } : i));
  };

  const updateItem = (id: string, field: string, value: number | string) => {
    setItems(items.map(i => i.id === id ? { ...i, [field]: value } : i));
  };

  // Calculations
  const subtotal = items.reduce((acc, item) => acc + (item.price * item.quantity), 0);
  const lineDiscounts = items.reduce((acc, item) => acc + ((item.price * item.quantity) * (item.discount / 100)), 0);
  const totalAfterLineDiscounts = subtotal - lineDiscounts;
  const generalDiscount = 0; // Keeping it 0 for this demo
  const totalBeforeVat = totalAfterLineDiscounts - generalDiscount;
  
  const vat = items.reduce((acc, item) => {
    const itemTotal = (item.price * item.quantity) * (1 - item.discount / 100);
    return acc + (itemTotal * (item.vat / 100));
  }, 0);

  const withholding = 0;
  const shipping = 0; 
  const grandTotal = totalBeforeVat + vat - withholding + shipping;

  const formatMoney = (val: number) => `₺${val.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

  return (
    <div className="flex flex-col lg:flex-row gap-6 pb-24 h-full">
      {/* Order Lines Area */}
      <div className="flex-1 flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold text-white tracking-tight">Order Lines</h2>
            <p className="text-xs text-slate-500 mt-0.5">Add and manage the products or services for this order.</p>
          </div>
          <button 
            onClick={addItem}
            className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium rounded-md transition-colors flex items-center gap-1.5 shadow-sm shadow-indigo-500/20"
          >
            <Plus size={16} />
            Add Item
          </button>
        </div>

        <div className="bg-[#1b202e] border border-[#2a3143] rounded-xl overflow-hidden flex flex-col shadow-sm">
          {/* Table Header */}
          <div className="hidden lg:grid grid-cols-12 gap-3 px-4 py-3 bg-[#1a1f2c] border-b border-[#2a3143] text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
            <div className="col-span-5">Product</div>
            <div className="col-span-2 text-right">Quantity</div>
            <div className="col-span-2 text-right">Unit Price</div>
            <div className="col-span-1 text-right">Disc %</div>
            <div className="col-span-1 text-right">VAT %</div>
            <div className="col-span-1 text-center">Actions</div>
          </div>

          {/* Table Body */}
          <div className="divide-y divide-[#2a3143]">
            <AnimatePresence>
              {items.length === 0 ? (
                <motion.div 
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: 'auto' }}
                  exit={{ opacity: 0, height: 0 }}
                  className="p-12 flex flex-col items-center justify-center text-slate-400 bg-[#171b26]"
                >
                  <div className="w-12 h-12 rounded-full bg-[#1b202e] border border-[#2a3143] flex items-center justify-center mb-4">
                    <ShoppingCart size={20} className="text-slate-500" />
                  </div>
                  <p className="text-sm font-medium text-slate-300 mb-1">No items added yet</p>
                  <p className="text-xs text-slate-500 mb-5 max-w-xs text-center">Add products or services to this order to calculate the final amount.</p>
                  <button 
                    onClick={addItem}
                    className="px-4 py-2 bg-[#1b202e] hover:bg-[#2a3143] border border-[#2a3143] text-slate-200 text-sm font-medium rounded-md transition-colors shadow-sm flex items-center gap-2"
                  >
                    <Plus size={16} /> Add First Item
                  </button>
                </motion.div>
              ) : (
                items.map((item) => (
                  <motion.div 
                    key={item.id}
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, scale: 0.95 }}
                    className={`p-4 transition-colors group ${item.expanded ? 'bg-[#1e2332]' : 'bg-[#1b202e] hover:bg-[#1f2536]'}`}
                  >
                    {/* Mobile/Desktop Layout Adaptability */}
                    <div className="grid grid-cols-1 lg:grid-cols-12 gap-3 lg:items-center">
                      <div className="col-span-1 lg:col-span-5 flex items-center gap-3">
                        <div className="cursor-grab text-slate-600 hover:text-slate-400 hidden lg:block">
                          <GripVertical size={16} />
                        </div>
                        <div className="flex-1">
                          <select 
                            value={item.product}
                            onChange={(e) => updateItem(item.id, 'product', e.target.value)}
                            className="w-full bg-[#0f111a] border border-[#2a3143] rounded-md px-3 py-1.5 text-sm text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all appearance-none cursor-pointer"
                          >
                            <option value="">Select Product...</option>
                            <option value="1">Fertilizer A (50kg)</option>
                            <option value="2">Tractor Part B</option>
                            <option value="3">Consulting Service - Hourly</option>
                          </select>
                        </div>
                      </div>

                      <div className="col-span-1 lg:col-span-6 grid grid-cols-6 gap-3">
                        <div className="col-span-2">
                          <label className="text-[10px] text-slate-500 uppercase font-semibold mb-1 block lg:hidden">Quantity</label>
                          <input 
                            type="number" 
                            value={item.quantity || ''} 
                            onChange={(e) => updateItem(item.id, 'quantity', parseFloat(e.target.value) || 0)}
                            className="w-full text-left lg:text-right bg-[#0f111a] border border-[#2a3143] rounded-md px-3 py-1.5 text-sm text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all" 
                          />
                        </div>
                        <div className="col-span-2">
                          <label className="text-[10px] text-slate-500 uppercase font-semibold mb-1 block lg:hidden">Unit Price</label>
                          <input 
                            type="number" 
                            value={item.price || ''} 
                            onChange={(e) => updateItem(item.id, 'price', parseFloat(e.target.value) || 0)}
                            className="w-full text-left lg:text-right bg-[#0f111a] border border-[#2a3143] rounded-md px-3 py-1.5 text-sm text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all" 
                          />
                        </div>
                        <div className="col-span-1">
                          <label className="text-[10px] text-slate-500 uppercase font-semibold mb-1 block lg:hidden">Disc %</label>
                          <input 
                            type="number" 
                            value={item.discount || ''} 
                            onChange={(e) => updateItem(item.id, 'discount', parseFloat(e.target.value) || 0)}
                            className="w-full text-left lg:text-right bg-[#0f111a] border border-[#2a3143] rounded-md px-2 py-1.5 text-sm text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all" 
                          />
                        </div>
                        <div className="col-span-1">
                          <label className="text-[10px] text-slate-500 uppercase font-semibold mb-1 block lg:hidden">VAT %</label>
                          <select 
                            value={item.vat}
                            onChange={(e) => updateItem(item.id, 'vat', parseFloat(e.target.value))}
                            className="w-full lg:text-right bg-[#0f111a] border border-[#2a3143] rounded-md px-1 py-1.5 text-sm text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all appearance-none cursor-pointer"
                          >
                            <option value="20">20</option>
                            <option value="10">10</option>
                            <option value="0">0</option>
                          </select>
                        </div>
                      </div>

                      <div className="col-span-1 flex items-center justify-end lg:justify-center gap-1 mt-2 lg:mt-0">
                        {confirmDeleteId === item.id ? (
                          <div className="flex items-center gap-1.5 bg-red-500/10 border border-red-500/20 pl-2 pr-1 py-1 rounded-md animate-in fade-in zoom-in-95 duration-200">
                            <span className="text-[10px] text-red-400 font-semibold uppercase tracking-wide whitespace-nowrap">Remove?</span>
                            <button onClick={() => removeItem(item.id)} className="text-[10px] font-medium text-white bg-red-500 hover:bg-red-600 px-2 py-0.5 rounded transition-colors">Yes</button>
                            <button onClick={() => setConfirmDeleteId(null)} className="text-[10px] font-medium text-slate-400 hover:text-slate-200 px-2 py-0.5 rounded transition-colors">No</button>
                          </div>
                        ) : (
                          <>
                            <button onClick={() => toggleExpand(item.id)} className={`transition-colors p-1.5 rounded-md ${item.expanded ? 'bg-indigo-500/10 text-indigo-400' : 'text-slate-500 hover:text-indigo-400 hover:bg-indigo-500/10'}`} title="Advanced Options">
                              <Settings2 size={16} />
                            </button>
                            <button onClick={() => setConfirmDeleteId(item.id)} className="text-slate-500 hover:text-red-400 hover:bg-red-500/10 transition-colors p-1.5 rounded-md" title="Remove Item">
                              <Trash2 size={16} />
                            </button>
                          </>
                        )}
                      </div>
                    </div>
                    
                    {/* Advanced Options & Line Total */}
                    <AnimatePresence>
                      {item.expanded && (
                        <motion.div 
                          initial={{ height: 0, opacity: 0 }}
                          animate={{ height: 'auto', opacity: 1 }}
                          exit={{ height: 0, opacity: 0 }}
                          className="overflow-hidden"
                        >
                          <div className="mt-4 lg:ml-7 grid grid-cols-1 sm:grid-cols-3 gap-4 p-4 bg-[#141824] rounded-lg border border-[#2a3143] shadow-inner">
                            <div className="flex flex-col gap-1.5">
                              <label className="text-[10px] uppercase text-slate-500 font-semibold tracking-wider">Withholding Tax</label>
                              <select className="w-full bg-[#0f111a] border border-[#2a3143] rounded-md text-xs text-slate-300 py-1.5 px-3 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 appearance-none cursor-pointer">
                                <option>No Withholding</option>
                                <option>1/2 Withholding</option>
                              </select>
                            </div>
                            <div className="flex flex-col gap-1.5">
                              <label className="text-[10px] uppercase text-slate-500 font-semibold tracking-wider">Warehouse</label>
                              <select className="w-full bg-[#0f111a] border border-[#2a3143] rounded-md text-xs text-slate-300 py-1.5 px-3 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 appearance-none cursor-pointer">
                                <option>Main Warehouse</option>
                                <option>Secondary Depot</option>
                              </select>
                            </div>
                            <div className="flex flex-col gap-1.5">
                              <label className="text-[10px] uppercase text-slate-500 font-semibold tracking-wider">Line Note</label>
                              <input type="text" placeholder="Optional note for this item..." className="w-full bg-[#0f111a] border border-[#2a3143] rounded-md text-xs text-slate-300 py-1.5 px-3 placeholder-slate-600 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500" />
                            </div>
                          </div>
                        </motion.div>
                      )}
                    </AnimatePresence>

                    <div className="mt-3 lg:ml-7 flex justify-end items-center text-xs text-slate-400 gap-2">
                      <span className="font-medium">Line Total:</span>
                      <span className="font-semibold text-slate-200 bg-[#141824] border border-[#2a3143] px-2 py-0.5 rounded">
                        {formatMoney((item.price * item.quantity) * (1 - item.discount / 100))}
                      </span>
                    </div>
                  </motion.div>
                ))
              )}
            </AnimatePresence>
          </div>
        </div>
      </div>

      {/* Financial Summary */}
      <div className="w-full lg:w-80 flex-shrink-0">
        <div className="bg-[#1b202e] border border-[#2a3143] rounded-xl overflow-hidden sticky top-6 shadow-sm">
          <div className="px-5 py-4 border-b border-[#2a3143] bg-[#1a1f2c]">
            <h3 className="text-sm font-semibold text-slate-200">Financial Summary</h3>
          </div>
          <div className="p-5 space-y-4">
            <div className="flex justify-between items-center text-sm">
              <span className="text-slate-400">Subtotal</span>
              <span className="text-slate-200 font-medium">{formatMoney(subtotal)}</span>
            </div>
            {lineDiscounts > 0 && (
              <div className="flex justify-between items-center text-sm">
                <span className="text-slate-400">Line Discounts</span>
                <span className="text-red-400 font-medium">- {formatMoney(lineDiscounts)}</span>
              </div>
            )}
            <div className="flex justify-between items-center text-sm">
              <span className="text-slate-400">General Discount</span>
              <span className="text-slate-500">- ₺0.00</span>
            </div>
            <div className="flex justify-between items-center text-sm">
              <span className="text-slate-400">VAT</span>
              <span className="text-slate-200 font-medium">{formatMoney(vat)}</span>
            </div>
            <div className="flex justify-between items-center text-sm">
              <span className="text-slate-400">Withholding</span>
              <span className="text-slate-500">- ₺0.00</span>
            </div>
            <div className="flex justify-between items-center text-sm">
              <span className="text-slate-400">Shipping</span>
              <span className="text-slate-500">₺0.00</span>
            </div>
            
            <div className="pt-5 border-t border-[#2a3143] mt-2">
              <div className="flex justify-between items-end mb-1.5">
                <span className="text-sm font-semibold text-slate-300">Grand Total</span>
                <span className="text-2xl font-bold text-white tracking-tight">{formatMoney(grandTotal)}</span>
              </div>
              <p className="text-[10px] text-slate-500 text-right">
                Estimated — calculated based on current inputs
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

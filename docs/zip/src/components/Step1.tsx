import { Calendar, Hash, Plus } from 'lucide-react';
import { FormInput, FormSelect, FormTextarea } from './FormControls';

export function Step1() {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 pb-24">
      {/* Left Column - Essential Info */}
      <div className="col-span-1 lg:col-span-8 space-y-6">
        
        {/* Customer & General */}
        <div className="bg-[#1b202e] rounded-xl border border-[#2a3143] overflow-hidden shadow-sm">
          <div className="px-5 py-4 border-b border-[#2a3143] bg-[#1a1f2c]">
            <h3 className="text-sm font-semibold text-slate-200">Customer & General Information</h3>
          </div>
          <div className="p-5 grid grid-cols-1 sm:grid-cols-2 gap-5">
            <div className="col-span-1 sm:col-span-2">
              <FormSelect 
                label="Customer" 
                required 
                options={[{value: '1', label: 'Anadolu Tarım Koop.'}]} 
                defaultValue="1"
              />
            </div>
            
            <FormInput 
              label="Order Number" 
              icon={Hash}
              placeholder="ORD-2026-00001"
              defaultValue="ORD-2026-00001"
            />
            <FormInput 
              label="Order Date" 
              type="date"
              icon={Calendar}
              defaultValue="2026-07-16"
            />

            <FormSelect 
              label="Order Type" 
              options={[{value: 'standard', label: 'Standard'}, {value: 'urgent', label: 'Urgent'}]} 
              defaultValue="standard"
            />
            <div className="grid grid-cols-2 gap-4">
              <FormSelect 
                label="Source" 
                options={[{value: 'manual', label: 'Manual'}, {value: 'web', label: 'Web'}]} 
                defaultValue="manual"
              />
              <FormSelect 
                label="Status" 
                options={[{value: 'draft', label: 'Draft'}, {value: 'confirmed', label: 'Confirmed'}]} 
                defaultValue="draft"
              />
            </div>
          </div>
        </div>

        {/* Addresses */}
        <div className="bg-[#1b202e] rounded-xl border border-[#2a3143] overflow-hidden shadow-sm">
          <div className="px-5 py-4 border-b border-[#2a3143] bg-[#1a1f2c]">
            <h3 className="text-sm font-semibold text-slate-200">Addresses & Delivery</h3>
          </div>
          <div className="p-5 grid grid-cols-1 sm:grid-cols-2 gap-5">
            <FormSelect 
              label="Billing Address" 
              options={[{value: 'main', label: 'Main Office - Istanbul, TR'}]} 
              defaultValue="main"
            />
            <FormSelect 
              label="Delivery Address" 
              options={[{value: 'main', label: 'Main Warehouse - Istanbul, TR'}]} 
              defaultValue="main"
            />
            <FormInput 
              label="Requested Delivery Date" 
              type="date"
              icon={Calendar}
            />
            <FormInput 
              label="Promised Delivery Date" 
              type="date"
              icon={Calendar}
            />
          </div>
        </div>

        {/* Notes */}
        <div className="bg-[#1b202e] rounded-xl border border-[#2a3143] overflow-hidden shadow-sm">
          <div className="px-5 py-4 border-b border-[#2a3143] bg-[#1a1f2c]">
            <h3 className="text-sm font-semibold text-slate-200">Notes</h3>
          </div>
          <div className="p-5 grid grid-cols-1 sm:grid-cols-2 gap-5">
            <FormTextarea label="Customer Notes" placeholder="These notes will be printed on the invoice..." />
            <FormTextarea label="Internal Notes" placeholder="Internal use only. Not visible to the customer." />
            <div className="col-span-1 sm:col-span-2">
              <FormTextarea label="General Notes" placeholder="Any additional details regarding this order..." />
            </div>
          </div>
        </div>

      </div>

      {/* Right Column - Commercial */}
      <div className="col-span-1 lg:col-span-4 space-y-6">
        <div className="bg-[#1b202e] rounded-xl border border-[#2a3143] overflow-hidden shadow-sm">
          <div className="px-5 py-4 border-b border-[#2a3143] bg-[#1a1f2c]">
            <h3 className="text-sm font-semibold text-slate-200">Commercial Conditions</h3>
          </div>
          <div className="p-5 space-y-5">
            <FormSelect 
              label="Currency" 
              options={[{value: 'try', label: 'TRY — Turkish Lira'}, {value: 'usd', label: 'USD — US Dollar'}]} 
              defaultValue="try"
            />
            <FormInput 
              label="Exchange Rate" 
              type="number"
              defaultValue="1.0000"
            />
            <FormSelect 
              label="Payment Terms" 
              options={[{value: '30', label: '30 Days Net'}, {value: 'cash', label: 'Cash in Advance'}]} 
              action={{ icon: <Plus size={12}/>, label: 'New' }}
              defaultValue="30"
            />
            <FormSelect 
              label="Price List" 
              options={[{value: 'standard', label: 'Standard Price List'}]} 
              action={{ icon: <Plus size={12}/>, label: 'New' }}
              defaultValue="standard"
            />
            <div className="grid grid-cols-2 gap-4">
              <FormInput 
                label="General Discount %" 
                type="number"
                placeholder="0.00"
              />
              <FormInput 
                label="Shipping Cost" 
                type="number"
                placeholder="0.00"
              />
            </div>
            <FormInput 
              label="Sales Channel" 
              placeholder="e.g. B2B Portal, Direct..."
            />
          </div>
        </div>
      </div>
    </div>
  );
}

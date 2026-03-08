import React, { useState } from 'react';
import { Heart, Users, MessageCircle, X, Send, ChevronUp } from 'lucide-react';

const ONLINE_USERS = [
    { id: 1, name: 'Sarah Connor', role: 'Sales Rep', avatar: 'SC' },
    { id: 2, name: 'John Smith', role: 'Manager', avatar: 'JS' },
    { id: 3, name: 'Mike Johnson', role: 'Support', avatar: 'MJ' },
    { id: 4, name: 'Emily Davis', role: 'Admin', avatar: 'ED' },
];

export const Footer: React.FC = () => {
    const [isUsersOpen, setIsUsersOpen] = useState(false);
    const [activeChat, setActiveChat] = useState<number | null>(null);
    const [chatMessage, setChatMessage] = useState('');

    const activeUser = ONLINE_USERS.find(u => u.id === activeChat);

    return (
        <>
            <footer className="shrink-0 border-t border-slate-200/60 dark:border-slate-800/60 bg-white dark:bg-[#0B0F19] py-3 text-xs text-slate-500 dark:text-slate-400 mt-auto z-10">
                <div className="flex items-center justify-between px-6">
                    <p>
                        &copy; {new Date().getFullYear()} CoreAlign. All rights reserved.
                    </p>
                    <div className="flex items-center gap-3">
                        <p className="flex items-center gap-1">
                            Made with <Heart size={12} className="text-red-500 fill-red-500" /> by Nexus Team
                        </p>

                        <button
                            onClick={() => setIsUsersOpen(!isUsersOpen)}
                            className={`flex items-center gap-2 px-3 py-1.5 rounded-[5px] transition-all border text-xs ${isUsersOpen
                                    ? 'bg-indigo-50 border-indigo-200 text-indigo-700 dark:bg-indigo-500/10 dark:border-indigo-500/30 dark:text-indigo-400'
                                    : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50 dark:bg-slate-800 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-700'
                                }`}
                        >
                            <div className="relative flex items-center justify-center">
                                <Users size={14} />
                                <div className="absolute -top-1 -right-1 w-2 h-2 bg-emerald-500 border border-white dark:border-slate-800 rounded-full" />
                            </div>
                            <span className="font-semibold">Online ({ONLINE_USERS.length})</span>
                            <ChevronUp size={14} className={`transition-transform duration-200 ${isUsersOpen ? 'rotate-180' : ''}`} />
                        </button>
                    </div>
                </div>
            </footer>

            {isUsersOpen && (
                <div className="fixed bottom-14 right-6 w-64 bg-white dark:bg-slate-800 rounded-[5px] shadow-2xl border border-slate-200 dark:border-slate-700 z-50 flex flex-col overflow-hidden">
                    <div className="bg-slate-50 dark:bg-slate-900/50 p-2.5 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between">
                        <h3 className="text-[11px] font-bold text-slate-900 dark:text-white flex items-center gap-1.5">
                            <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                            Online Users
                        </h3>
                        <button
                            onClick={() => setIsUsersOpen(false)}
                            className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors"
                        >
                            <X size={14} />
                        </button>
                    </div>

                    <div className="max-h-64 overflow-y-auto p-1.5 space-y-[2px]">
                        {ONLINE_USERS.map(user => (
                            <button
                                key={user.id}
                                onClick={() => {
                                    setActiveChat(user.id);
                                    setIsUsersOpen(false);
                                }}
                                className="w-full flex items-center gap-2 p-1.5 rounded-[5px] hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors text-left group"
                            >
                                <div className="relative">
                                    <div className="w-7 h-7 rounded-[5px] bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-white font-semibold text-[10px] shadow-sm">
                                        {user.avatar}
                                    </div>
                                    <div className="absolute -bottom-0.5 -right-0.5 w-2 h-2 bg-emerald-500 border border-white dark:border-slate-800 rounded-full" />
                                </div>
                                <div className="flex-1 min-w-0">
                                    <p className="text-[11px] font-semibold text-slate-900 dark:text-white truncate group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                                        {user.name}
                                    </p>
                                    <p className="text-[9px] text-slate-500 dark:text-slate-400 truncate">
                                        {user.role}
                                    </p>
                                </div>
                                <MessageCircle size={14} className="text-slate-300 dark:text-slate-500 opacity-0 group-hover:opacity-100 transition-opacity" />
                            </button>
                        ))}
                    </div>
                </div>
            )}

            {activeChat && activeUser && (
                <div className={`fixed bottom-4 ${isUsersOpen ? 'right-[300px]' : 'right-6'} w-72 bg-white dark:bg-slate-800 rounded-[5px] shadow-2xl border border-slate-200 dark:border-slate-700 z-[60] flex flex-col overflow-hidden transition-all`}>
                    <div className="bg-indigo-600 p-2.5 flex items-center justify-between text-white">
                        <div className="flex items-center gap-2.5">
                            <div className="relative">
                                <div className="w-7 h-7 rounded-[3px] bg-white/20 flex items-center justify-center text-[10px] font-bold">
                                    {activeUser.avatar}
                                </div>
                                <div className="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 bg-emerald-400 border-2 border-indigo-600 rounded-full" />
                            </div>
                            <div>
                                <p className="text-[12px] font-bold leading-tight">{activeUser.name}</p>
                                <p className="text-[10px] text-indigo-200 leading-tight">{activeUser.role}</p>
                            </div>
                        </div>
                        <button
                            onClick={() => setActiveChat(null)}
                            className="p-1 hover:bg-white/20 rounded-[3px] transition-colors"
                        >
                            <X size={14} />
                        </button>
                    </div>

                    <div className="h-56 p-3 bg-slate-50 dark:bg-slate-900/50 overflow-y-auto flex flex-col gap-2">
                        <div className="self-start bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 p-2 rounded-[5px] rounded-tl-none max-w-[85%] shadow-sm">
                            <p className="text-[11px] text-slate-700 dark:text-slate-300">Hi, I need help with order ORD-002.</p>
                            <span className="text-[9px] text-slate-400 mt-1 block">10:42 AM</span>
                        </div>
                    </div>

                    <div className="p-2 bg-white dark:bg-slate-800 border-t border-slate-200 dark:border-slate-700 flex items-center gap-1.5">
                        <input
                            type="text"
                            value={chatMessage}
                            onChange={(e) => setChatMessage(e.target.value)}
                            placeholder="Type a message..."
                            className="flex-1 bg-slate-100 dark:bg-slate-900 border-none rounded-[3px] px-2.5 py-2 text-[11px] text-slate-900 dark:text-white focus:ring-1 focus:ring-indigo-500 outline-none"
                        />
                        <button className="p-2 bg-indigo-600 text-white rounded-[3px] hover:bg-indigo-700 transition-colors shrink-0">
                            <Send size={14} />
                        </button>
                    </div>
                </div>
            )}
        </>
    );
};

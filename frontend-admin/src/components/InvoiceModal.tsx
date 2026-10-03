import React, { useState, useEffect } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import { 
  FileText, 
  X, 
  Plus, 
  Trash2, 
  ShieldCheck, 
  ShieldAlert, 
  Printer, 
  Save, 
  CheckCircle2, 
  AlertCircle,
  Cpu,
  User,
  Clock,
  Wrench,
  Receipt
} from 'lucide-react';
import { toast } from 'sonner';
import api from '../services/api';

interface InvoiceItemForm {
  id?: number;
  item_name: string;
  item_code?: string;
  category: 'sparepart' | 'service_fee' | 'diagnostic_fee' | 'transport_fee' | 'other';
  quantity: number;
  unit_price: number;
  is_covered_by_warranty: boolean;
  notes?: string;
}

interface InvoiceModalProps {
  isOpen: boolean;
  onClose: () => void;
  repairRequest: any | null;
  onSaved?: () => void;
}

export const InvoiceModal: React.FC<InvoiceModalProps> = ({
  isOpen,
  onClose,
  repairRequest,
  onSaved
}) => {
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [invoice, setInvoice] = useState<any | null>(null);

  const [items, setItems] = useState<InvoiceItemForm[]>([
    {
      item_name: 'Jasa Diagnostik & Servis Hardware Resmi',
      category: 'service_fee',
      quantity: 1,
      unit_price: 250000,
      is_covered_by_warranty: true,
      notes: 'Pemeriksaan menyeluruh & verifikasi kestabilan perangkat'
    },
    {
      item_name: 'Modul Suku Cadang Resmi JTS (Replacement Part)',
      item_code: 'JTS-PART-OEM-01',
      category: 'sparepart',
      quantity: 1,
      unit_price: 1500000,
      is_covered_by_warranty: true,
      notes: 'Garansi resmi 1 tahun'
    }
  ]);

  const [notes, setNotes] = useState('Faktur jaminan garansi resmi PT Jaya Teknologi Solusindo.');
  const [terms, setTerms] = useState('1. Suku cadang resmi digaransi selama 1 tahun.\n2. Biaya yang dijamin garansi resmi telah dipotong 100%.\n3. Harap simpan faktur ini sebagai bukti klaim sah.');

  const fetchInvoice = async () => {
    if (!repairRequest) return;
    setLoading(true);
    try {
      const res = await api.get(`/repair-requests/${repairRequest.id}/invoice`);
      if (res.data?.has_invoice && res.data?.invoice) {
        const inv = res.data.invoice;
        setInvoice(inv);
        if (inv.items && inv.items.length > 0) {
          setItems(inv.items.map((i: any) => ({
            id: i.id,
            item_name: i.item_name,
            item_code: i.item_code || '',
            category: i.category,
            quantity: i.quantity,
            unit_price: parseFloat(i.unit_price) || 0,
            is_covered_by_warranty: Boolean(i.is_covered_by_warranty),
            notes: i.notes || ''
          })));
        }
        if (inv.notes) setNotes(inv.notes);
        if (inv.terms_and_conditions) setTerms(inv.terms_and_conditions);
      } else {
        setInvoice(null);
      }
    } catch (e: any) {
      // Not found is normal for new tickets
      setInvoice(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen && repairRequest) {
      fetchInvoice();
    }
  }, [isOpen, repairRequest]);

  const handleAddItem = () => {
    setItems([
      ...items,
      {
        item_name: '',
        category: 'sparepart',
        quantity: 1,
        unit_price: 0,
        is_covered_by_warranty: true,
        notes: ''
      }
    ]);
  };

  const handleRemoveItem = (index: number) => {
    if (items.length <= 1) {
      toast.error('Invoice harus memiliki minimal 1 item.');
      return;
    }
    setItems(items.filter((_, idx) => idx !== index));
  };

  const handleItemChange = (index: number, field: keyof InvoiceItemForm, value: any) => {
    const updated = [...items];
    updated[index] = { ...updated[index], [field]: value };
    setItems(updated);
  };

  // Calculations
  const grossSubtotal = items.reduce((acc, item) => acc + (item.quantity * (item.unit_price || 0)), 0);
  const warrantyDiscount = items.reduce((acc, item) => {
    return item.is_covered_by_warranty ? acc + (item.quantity * (item.unit_price || 0)) : acc;
  }, 0);
  const netPayable = Math.max(0, grossSubtotal - warrantyDiscount);

  const handleSaveInvoice = async () => {
    if (!repairRequest) return;
    for (const item of items) {
      if (!item.item_name.trim()) {
        toast.error('Nama item tidak boleh kosong.');
        return;
      }
    }

    setSaving(true);
    try {
      const payload = {
        notes,
        terms_and_conditions: terms,
        items: items.map((i) => ({
          item_name: i.item_name,
          item_code: i.item_code || null,
          category: i.category,
          quantity: Number(i.quantity),
          unit_price: Number(i.unit_price),
          is_covered_by_warranty: Boolean(i.is_covered_by_warranty),
          notes: i.notes || null,
        }))
      };

      const res = await api.post(`/repair-requests/${repairRequest.id}/invoice`, payload);
      setInvoice(res.data.invoice);
      toast.success('Faktur perbaikan & jaminan garansi berhasil disimpan!');
      if (onSaved) onSaved();
    } catch (e: any) {
      console.error(e);
      toast.error(e.response?.data?.message || 'Gagal menyimpan invoice.');
    } finally {
      setSaving(false);
    }
  };

  const handlePrint = () => {
    if (!invoice?.id) {
      toast.error('Simpan invoice terlebih dahulu sebelum mencetak.');
      return;
    }
    window.open(`/api/invoices/${invoice.id}/print`, '_blank');
  };

  if (!repairRequest) return null;

  const device = repairRequest.device;
  const user = repairRequest.user;
  const warranty = device?.warranty;

  return (
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog as="div" className="relative z-50 font-sans text-ink" onClose={onClose}>
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-200"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-150"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-ink/50 backdrop-blur-sm" />
        </Transition.Child>

        <div className="fixed inset-0 overflow-y-auto">
          <div className="flex min-h-full items-center justify-center p-4 text-center">
            <Transition.Child
              as={Fragment}
              enter="ease-out duration-200"
              enterFrom="opacity-0 scale-95"
              enterTo="opacity-100 scale-100"
              leave="ease-in duration-150"
              leaveFrom="opacity-100 scale-100"
              leaveTo="opacity-0 scale-95"
            >
              <Dialog.Panel className="w-full max-w-4xl transform overflow-hidden rounded-card bg-paper-card text-left align-middle shadow-modal border border-mist transition-all">
                
                {/* Header */}
                <div className="flex items-center justify-between px-6 py-4 border-b border-mist bg-paper">
                  <div className="flex items-center gap-3">
                    <div className="p-2 rounded-btn bg-circuit text-white">
                      <Receipt className="w-5 h-5" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-mono text-sm font-bold text-circuit">
                          Faktur &amp; Klaim Garansi Servis #{repairRequest.id}
                        </span>
                        {invoice?.invoice_number && (
                          <span className="text-xs px-2 py-0.5 rounded-full font-mono font-semibold bg-signal/10 text-signal border border-signal/20">
                            {invoice.invoice_number}
                          </span>
                        )}
                      </div>
                      <p className="text-xs text-ink-muted">
                        Manajemen rincian biaya, pemotongan garansi resmi 100%, dan cetak bukti perbaikan.
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {invoice && (
                      <button
                        onClick={handlePrint}
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-signal text-white rounded-btn text-xs font-semibold hover:bg-signal-hover shadow-sm transition-all"
                      >
                        <Printer className="w-3.5 h-3.5" />
                        <span>Cetak / PDF</span>
                      </button>
                    )}
                    <button
                      onClick={onClose}
                      className="p-1.5 rounded-btn text-ink-subtle hover:text-ink hover:bg-mist transition-colors"
                    >
                      <X className="w-5 h-5" />
                    </button>
                  </div>
                </div>

                {/* Content */}
                <div className="p-6 space-y-6 max-h-[75vh] overflow-y-auto">
                  
                  {/* Info Header Card */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4 bg-mist-light p-4 rounded-card border border-mist text-xs">
                    <div>
                      <div className="font-bold text-ink flex items-center gap-1.5 mb-1.5">
                        <User className="w-3.5 h-3.5 text-circuit" />
                        <span>Data Klien / Pemilik</span>
                      </div>
                      <div className="space-y-0.5 text-ink-muted">
                        <p><span className="font-medium text-ink">Nama:</span> {user?.name || '-'}</p>
                        <p><span className="font-medium text-ink">Kontak / WA:</span> {user?.phone || user?.email || '-'}</p>
                        <p><span className="font-medium text-ink">Alamat:</span> {user?.address || 'Terkalibrasi GPS'}</p>
                      </div>
                    </div>
                    <div>
                      <div className="font-bold text-ink flex items-center gap-1.5 mb-1.5">
                        <Cpu className="w-3.5 h-3.5 text-circuit" />
                        <span>Data Unit &amp; Status Garansi</span>
                      </div>
                      <div className="space-y-0.5 text-ink-muted">
                        <p><span className="font-medium text-ink">Model:</span> {device?.model || '-'}</p>
                        <p><span className="font-medium text-ink">Serial:</span> <code className="text-circuit font-bold">{device?.serial_number || '-'}</code></p>
                        <p className="flex items-center gap-1.5">
                          <span className="font-medium text-ink">Garansi:</span>
                          {warranty?.status === 'active' ? (
                            <span className="inline-flex items-center gap-1 px-1.5 py-0.2 rounded bg-stable/10 text-stable font-semibold text-[11px]">
                              <ShieldCheck className="w-3 h-3" /> Aktif Hingga {warranty.warranty_end || '2028'}
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 px-1.5 py-0.2 rounded bg-alert/10 text-alert font-semibold text-[11px]">
                              <ShieldAlert className="w-3 h-3" /> Non-Aktif / Kedaluwarsa
                            </span>
                          )}
                        </p>
                      </div>
                    </div>
                  </div>

                  {/* Items Editor */}
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <div>
                        <h3 className="text-sm font-bold text-ink flex items-center gap-2">
                          <span>Rincian Tindakan, Jasa &amp; Suku Cadang</span>
                        </h3>
                        <p className="text-xs text-ink-muted">
                          Tentukan apakah tiap item dilindungi garansi resmi (Diskon 100% / Rp 0) atau ditagihkan ke klien.
                        </p>
                      </div>
                      <button
                        onClick={handleAddItem}
                        className="inline-flex items-center gap-1 px-2.5 py-1.5 bg-paper border border-mist rounded-btn text-xs font-semibold text-circuit hover:bg-mist-light transition-colors"
                      >
                        <Plus className="w-3.5 h-3.5" />
                        <span>Tambah Item</span>
                      </button>
                    </div>

                    <div className="border border-mist rounded-card overflow-hidden">
                      <table className="w-full text-xs text-left">
                        <thead className="bg-paper text-ink-muted uppercase text-[10px] font-bold border-b border-mist">
                          <tr>
                            <th className="py-2.5 px-3">Item / Tindakan</th>
                            <th className="py-2.5 px-3 w-28">Kategori</th>
                            <th className="py-2.5 px-3 w-16 text-center">Qty</th>
                            <th className="py-2.5 px-3 w-32 text-right">Harga Satuan (Rp)</th>
                            <th className="py-2.5 px-3 w-36 text-center">Status Garansi</th>
                            <th className="py-2.5 px-3 w-32 text-right">Ditagih Klien</th>
                            <th className="py-2.5 px-2 w-10 text-center"></th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-mist">
                          {items.map((item, idx) => {
                            const subtotal = item.quantity * (item.unit_price || 0);
                            const payable = item.is_covered_by_warranty ? 0 : subtotal;

                            return (
                              <tr key={idx} className="hover:bg-mist-light/50 transition-colors">
                                <td className="p-2.5 space-y-1">
                                  <input
                                    type="text"
                                    value={item.item_name}
                                    onChange={(e) => handleItemChange(idx, 'item_name', e.target.value)}
                                    placeholder="Contoh: Penggantian Layar LCD IPS"
                                    className="w-full px-2 py-1 bg-white border border-mist rounded text-xs text-ink font-medium focus:outline-none focus:border-circuit"
                                  />
                                  <div className="flex gap-2">
                                    <input
                                      type="text"
                                      value={item.item_code || ''}
                                      onChange={(e) => handleItemChange(idx, 'item_code', e.target.value)}
                                      placeholder="Part No. (Opsional)"
                                      className="w-1/3 px-2 py-0.5 bg-paper border border-mist rounded text-[11px] text-ink-muted"
                                    />
                                    <input
                                      type="text"
                                      value={item.notes || ''}
                                      onChange={(e) => handleItemChange(idx, 'notes', e.target.value)}
                                      placeholder="Catatan teknisi..."
                                      className="w-2/3 px-2 py-0.5 bg-paper border border-mist rounded text-[11px] text-ink-muted"
                                    />
                                  </div>
                                </td>
                                <td className="p-2.5 align-top">
                                  <select
                                    value={item.category}
                                    onChange={(e) => handleItemChange(idx, 'category', e.target.value)}
                                    className="w-full px-2 py-1 bg-white border border-mist rounded text-xs text-ink focus:outline-none"
                                  >
                                    <option value="sparepart">Sparepart</option>
                                    <option value="service_fee">Jasa Servis</option>
                                    <option value="diagnostic_fee">Diagnostik</option>
                                    <option value="transport_fee">Transport On-Site</option>
                                    <option value="other">Lainnya</option>
                                  </select>
                                </td>
                                <td className="p-2.5 align-top text-center">
                                  <input
                                    type="number"
                                    min="1"
                                    value={item.quantity}
                                    onChange={(e) => handleItemChange(idx, 'quantity', parseInt(e.target.value) || 1)}
                                    className="w-12 text-center px-1.5 py-1 bg-white border border-mist rounded text-xs text-ink focus:outline-none"
                                  />
                                </td>
                                <td className="p-2.5 align-top text-right">
                                  <input
                                    type="number"
                                    min="0"
                                    step="10000"
                                    value={item.unit_price}
                                    onChange={(e) => handleItemChange(idx, 'unit_price', parseFloat(e.target.value) || 0)}
                                    className="w-full text-right px-2 py-1 bg-white border border-mist rounded text-xs font-mono text-ink focus:outline-none"
                                  />
                                </td>
                                <td className="p-2.5 align-top text-center">
                                  <button
                                    type="button"
                                    onClick={() => handleItemChange(idx, 'is_covered_by_warranty', !item.is_covered_by_warranty)}
                                    className={`w-full px-2 py-1 rounded text-[11px] font-bold transition-colors flex items-center justify-center gap-1 border ${
                                      item.is_covered_by_warranty 
                                        ? 'bg-stable-subtle text-stable border-stable/30' 
                                        : 'bg-alert-subtle text-alert border-alert-border'
                                    }`}
                                  >
                                    {item.is_covered_by_warranty ? (
                                      <>
                                        <ShieldCheck className="w-3 h-3" />
                                        <span>🛡️ Garansi (0)</span>
                                      </>
                                    ) : (
                                      <>
                                        <ShieldAlert className="w-3 h-3" />
                                        <span>⚠️ Bayar Klien</span>
                                      </>
                                    )}
                                  </button>
                                </td>
                                <td className="p-2.5 align-top text-right font-mono font-bold">
                                  {item.is_covered_by_warranty ? (
                                    <span className="text-stable">Rp 0 (Covered)</span>
                                  ) : (
                                    <span className="text-fault">Rp {payable.toLocaleString('id-ID')}</span>
                                  )}
                                </td>
                                <td className="p-2.5 align-top text-center">
                                  <button
                                    type="button"
                                    onClick={() => handleRemoveItem(idx)}
                                    className="p-1 text-ink-subtle hover:text-fault transition-colors"
                                    title="Hapus baris"
                                  >
                                    <Trash2 className="w-3.5 h-3.5" />
                                  </button>
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>
                  </div>

                  {/* Summary Box */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6 bg-paper p-4 rounded-card border border-mist">
                    <div className="space-y-2">
                      <label className="text-xs font-bold text-ink">Catatan Faktur</label>
                      <textarea
                        rows={2}
                        value={notes}
                        onChange={(e) => setNotes(e.target.value)}
                        className="w-full p-2 bg-white border border-mist rounded text-xs text-ink focus:outline-none"
                        placeholder="Catatan pengerjaan atau pesan ke klien..."
                      />
                    </div>

                    <div className="space-y-2 text-xs">
                      <div className="flex justify-between text-ink-muted">
                        <span>Total Nilai Pengerjaan (Gross):</span>
                        <span className="font-mono font-bold text-ink">Rp {grossSubtotal.toLocaleString('id-ID')}</span>
                      </div>
                      <div className="flex justify-between text-stable font-semibold">
                        <span className="flex items-center gap-1">
                          <ShieldCheck className="w-3.5 h-3.5" />
                          <span>Jaminan Garansi Resmi PT JTS:</span>
                        </span>
                        <span className="font-mono font-bold">- Rp {warrantyDiscount.toLocaleString('id-ID')}</span>
                      </div>
                      <div className="pt-2 border-t border-mist flex justify-between items-center text-sm">
                        <span className="font-bold text-ink">Sisa Tagihan Klien (Net):</span>
                        <span className={`font-mono font-extrabold ${netPayable === 0 ? 'text-stable text-base' : 'text-fault text-base'}`}>
                          {netPayable === 0 ? 'Rp 0 (LUNAS GARANSI)' : `Rp ${netPayable.toLocaleString('id-ID')}`}
                        </span>
                      </div>
                      {netPayable === 0 ? (
                        <div className="p-2 rounded bg-stable-subtle border border-stable/30 text-stable text-[11px] font-semibold flex items-center gap-1.5">
                          <CheckCircle2 className="w-3.5 h-3.5 flex-shrink-0" />
                          <span>Seluruh tindakan &amp; suku cadang dijamin 100% oleh Garansi Resmi PT JTS. Klien tidak dikenakan biaya.</span>
                        </div>
                      ) : (
                        <div className="p-2 rounded bg-alert-subtle border border-alert-border text-alert text-[11px] font-semibold flex items-center gap-1.5">
                          <AlertCircle className="w-3.5 h-3.5 flex-shrink-0" />
                          <span>Terdapat item perbaikan di luar cakupan garansi yang perlu dilunasi oleh klien.</span>
                        </div>
                      )}
                    </div>
                  </div>

                </div>

                {/* Footer Actions */}
                <div className="flex items-center justify-between px-6 py-4 border-t border-mist bg-paper">
                  <div className="text-xs text-ink-muted">
                    {invoice ? (
                      <span className="text-stable font-semibold flex items-center gap-1">
                        <CheckCircle2 className="w-3.5 h-3.5" />
                        Invoice tersimpan di server
                      </span>
                    ) : (
                      <span>Draft baru belum disimpan</span>
                    )}
                  </div>
                  <div className="flex items-center gap-3">
                    <button
                      type="button"
                      onClick={onClose}
                      className="px-4 py-2 border border-mist text-ink font-medium rounded-btn text-xs hover:bg-mist transition-colors"
                    >
                      Batal
                    </button>
                    <button
                      type="button"
                      onClick={handleSaveInvoice}
                      disabled={saving}
                      className="inline-flex items-center gap-2 px-5 py-2 bg-circuit text-white font-bold rounded-btn text-xs hover:bg-circuit-dark transition-all disabled:opacity-50"
                    >
                      <Save className="w-3.5 h-3.5" />
                      <span>{saving ? 'Menyimpan...' : 'Simpan Faktur Servis'}</span>
                    </button>
                  </div>
                </div>

              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
};

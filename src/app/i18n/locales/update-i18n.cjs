const fs = require('fs');
const path = require('path');

const productionJobsKeysEn = {
  title: "Production Jobs",
  subtitle: "Manage production travelers, operations, and shop floor execution.",
  empty_title: "No production jobs found",
  empty_desc: "Create a job to start manufacturing products.",
  actions: {
    new_job: "New Job",
    release: "Release Job",
    start: "Start",
    finish: "Finish",
    complete_job: "Complete Job"
  },
  status: {
    all: "All",
    Draft: "Draft",
    Released: "Released",
    InProgress: "In Progress",
    OnHold: "On Hold",
    ReadyToComplete: "Ready To Complete",
    Completed: "Completed",
    Cancelled: "Cancelled"
  },
  stepStatus: {
    Pending: "Pending",
    InProgress: "In Progress",
    Completed: "Completed",
    Skipped: "Skipped",
    Reopened: "Reopened"
  },
  fields: {
    product: "Product",
    qty: "Quantity",
    completed: "Completed",
    uom: "Unit",
    routing: "Routing",
    warehouse: "Warehouse",
    notes: "Notes",
    dueDate: "Due Date",
    progress: "Progress",
    good: "Good Qty",
    scrapped: "Scrap Qty",
    warehouse_required: "Warehouse selection is required"
  },
  cancel_title: "Cancel Job",
  cancel_message: "Are you sure you want to cancel job {{number}}?",
  cancel_success: "Job cancelled successfully",
  cancel_error: "Could not cancel job",
  create_success: "Job created successfully",
  create_error: "Could not create job",
  release_success: "Job released to shop floor",
  release_error: "Could not release job",
  complete_success: "Job completed successfully",
  complete_error: "Could not complete job",
  traveler_steps: "Traveler Steps",
  no_steps: "No routing steps defined for this job.",
  step: "Step",
  select_operator: "Select Operator...",
  operator_required: "Operator selection is required",
  step_started: "Operation started",
  step_finished: "Operation finished",
  step_skipped: "Operation skipped",
  ready_to_complete_title: "All Steps Completed",
  ready_to_complete_desc: "All required operations are done. The job is ready to be marked as complete and stock to be received."
};

const productionJobsKeysTr = {
  title: "Üretim İşleri",
  subtitle: "Üretim emirlerini, operasyonları ve saha yürütmesini yönetin.",
  empty_title: "Üretim işi bulunamadı",
  empty_desc: "Üretime başlamak için yeni bir iş oluşturun.",
  actions: {
    new_job: "Yeni İş",
    release: "İşi Başlat (Release)",
    start: "Başla",
    finish: "Bitir",
    complete_job: "İşi Tamamla"
  },
  status: {
    all: "Tümü",
    Draft: "Taslak",
    Released: "Sahada",
    InProgress: "Devam Ediyor",
    OnHold: "Beklemede",
    ReadyToComplete: "Tamamlanmaya Hazır",
    Completed: "Tamamlandı",
    Cancelled: "İptal Edildi"
  },
  stepStatus: {
    Pending: "Bekliyor",
    InProgress: "Devam Ediyor",
    Completed: "Tamamlandı",
    Skipped: "Atlandı",
    Reopened: "Yeniden Açıldı"
  },
  fields: {
    product: "Ürün",
    qty: "Miktar",
    completed: "Tamamlanan",
    uom: "Birim",
    routing: "Rota (Routing)",
    warehouse: "Depo",
    notes: "Notlar",
    dueDate: "Termin",
    progress: "İlerleme",
    good: "Sağlam Miktar",
    scrapped: "Fire Miktar",
    warehouse_required: "Depo seçimi zorunludur"
  },
  cancel_title: "İşi İptal Et",
  cancel_message: "{{number}} numaralı işi iptal etmek istediğinize emin misiniz?",
  cancel_success: "İş başarıyla iptal edildi",
  cancel_error: "İş iptal edilemedi",
  create_success: "İş başarıyla oluşturuldu",
  create_error: "İş oluşturulamadı",
  release_success: "İş üretim sahasına alındı",
  release_error: "İş sahaya alınamadı",
  complete_success: "İş başarıyla tamamlandı",
  complete_error: "İş tamamlanamadı",
  traveler_steps: "Operasyon Adımları",
  no_steps: "Bu iş için rota adımı tanımlanmamış.",
  step: "Adım",
  select_operator: "Operatör Seçin...",
  operator_required: "Operatör seçimi zorunludur",
  step_started: "Operasyon başlatıldı",
  step_finished: "Operasyon bitirildi",
  step_skipped: "Operasyon atlandı",
  ready_to_complete_title: "Tüm Adımlar Tamamlandı",
  ready_to_complete_desc: "Tüm zorunlu operasyonlar bitti. İş tamamlandı olarak işaretlenebilir ve stok girişi yapılabilir."
};

function updateFile(filename, keys) {
  const filepath = path.join(__dirname, filename);
  const data = JSON.parse(fs.readFileSync(filepath, 'utf8'));
  data['ProductionJobs'] = keys;
  fs.writeFileSync(filepath, JSON.stringify(data, null, 2));
}

updateFile('en.json', productionJobsKeysEn);
updateFile('tr.json', productionJobsKeysTr);
console.log("Updated both json files");

Route: /dashboard/invoices

# Fatura oluşturma

Fatura iki şekilde oluşturulur. Mevcut bir siparişi faturalandırmak için Siparişler sayfasını açın, siparişi bulun ve "Fatura oluştur"a tıklayıp çıkan onayı verin; CoreAlign faturayı siparişin satırlarından üretir (POST /invoices/from-order/{orderId}).

Siparişe bağlı olmayan bir fatura kesmek için Faturalar sayfasını açın ve sağ üstteki "Yeni fatura"ya tıklayın. Müşteriyi seçin; para birimini, fatura tarihini ve vadeyi (gün) belirleyin; satır kalemlerini (stok kodu, kalem adı, miktar, birim fiyat, KDV oranı) ekleyin ve "Oluştur"a tıklayın (POST /invoices/standalone).

Yeni fatura anında kesilir ve müşteri defterine ve muhasebeye işlenir. Fatura detayından ödeme alabilir, faturayı yazdırabilir veya alacak dekontu kesebilirsiniz.

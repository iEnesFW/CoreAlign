Route: /dashboard/invoices

# Alacak dekontu kesme

Alacak dekontu, kesilmiş bir faturayı tamamen veya kısmen tersine çevirir. İki yolla kesilebilir.

Doğrudan faturadan: Faturalar sayfasını açın, alacaklandırmak istediğiniz faturayı açın ve "Alacak dekontu kes"e tıklayın. Alacaklandırılacak satırları işaretleyin, miktarları ayarlayın, isterseniz bir gerekçe girin ve "Alacak Dekontu Oluştur"a tıklayın (POST /invoices/{id}/credit-notes). Bu işlem, fatura kesildikten sonra (iptal edilmemiş ve geçersiz kılınmamış faturalarda) kullanılabilir.

İadeden: İadeler sayfasında onaylı bir iadeyi "Alacak dekontunu otomatik oluştur" işaretliyken teslim aldığınızda CoreAlign iade edilen satırlar için kaynak faturayı tersine çevirir ve alacak dekontunu sizin yerinize keser. Dekont numarası ve kaynak fatura ardından iade detayında görünür.

/*
 * Projekt z przedmiotu Zastosowania Procesorów Sygnałowych
 * Szablon projektu dla DSP TMS320C5535
 */


// Dołączenie wszelkich potrzebnych plików nagłówkowych
#include "usbstk5515.h"
#include "usbstk5515_led.h"
#include "aic3204.h"
#include "PLL.h"
#include "bargraph.h"
#include "oled.h"
#include "pushbuttons.h"
#include "dsplib.h"
#include "testsignal.h"
#include "okno.h"

//linia DATA_SECTION
#pragma DATA_SECTION (bufor, ".input")
#pragma DATA_SECTION (bufor_okno, ".input")

//indeks do maksimum
int maxIndex;

//szukana czestotliwosci
int czestotliwosc;

//minimalny prog do obliczenia maksimum
int progMinimalny = 500;

//tablica typu short i indeks do niej
#define NBUF 2048
short bufor[NBUF]
int index = 0;

short bufor_okno[NBUF]
int index = 0;

//liczba zespolona
int realis = 0;
int imaginalis = 0;


// Częstotliwość próbkowania (48 kHz)
#define CZESTOTL_PROBKOWANIA 48000L
// Wzmocnienie wejścia w dB: 0 dla wejścia liniowego, 30 dla mikrofonowego
#define WZMOCNIENIE_WEJSCIA_dB 30


// Główna procedura programu

void main(void) {

	// Inicjalizacja układu DSP
	USBSTK5515_init();			// BSL
	pll_frequency_setup(100);	// PLL 100 MHz
	aic3204_hardware_init();	// I2C
	aic3204_init();				// kodek dźwięku AIC3204
	USBSTK5515_ULED_init();		// diody LED
	SAR_init_pushbuttons();		// przyciski
	oled_init();				// wyświelacz LED 2x19 znaków

	// ustawienie częstotliwości próbkowania i wzmocnienia wejścia
	set_sampling_frequency_and_gain(CZESTOTL_PROBKOWANIA, WZMOCNIENIE_WEJSCIA_dB);

	// wypisanie komunikatu na wyświetlaczu
	// 2 linijki po 19 znaków, tylko wielkie angielskie litery
	oled_display_message("PROJEKT ZPS        ", "                   ");

	// 'tryb_pracy' oznacza tryb pracy wybrany przyciskami
	unsigned int tryb_pracy = 0;
	unsigned int poprzedni_tryb_pracy = 99;

	// zmienne do przechowywania wartości próbek
	short lewy_wejscie, prawy_wejscie, lewy_wyjscie, prawy_wyjscie;

	// Przetwarzanie próbek sygnału w pętli
	while (1) {

		// odczyt próbek audio, zamiana na mono
		aic3204_codec_read(&lewy_wejscie, &prawy_wejscie);
		short mono_wejscie = (lewy_wejscie >> 1) + (prawy_wejscie >> 1);

		// sprawdzamy czy wciśnięto przycisk
		// argument: maksymalna liczba obsługiwanych trybów
		tryb_pracy = pushbuttons_read(4);
		if (tryb_pracy == 0) // oba wciśnięte - wyjście
			break;
		else if (tryb_pracy != poprzedni_tryb_pracy) {
			// nastąpiła zmiana trybu - wciśnięto przycisk
			USBSTK5515_ULED_setall(0x0F); // zgaszenie wszystkich diód
			if (tryb_pracy == 1) {
				// wypisanie informascji na wyświetlaczu
				oled_display_message("PROJEKT ZPS        ", "TRYB 1             ");
				// zapalenie diody nr 1
				USBSTK5515_ULED_on(0);
			} else if (tryb_pracy == 2) {
				oled_display_message("PROJEKT ZPS        ", "TRYB 2             ");
				USBSTK5515_ULED_on(1);
			} else if (tryb_pracy == 3) {
				oled_display_message("PROJEKT ZPS        ", "TRYB 3             ");
				USBSTK5515_ULED_on(2);
			} else if (tryb_pracy == 4) {
				oled_display_message("PROJEKT ZPS        ", "TRYB 4             ");
				USBSTK5515_ULED_on(3);
			}
			// zapisujemy nowo ustawiony tryb
			poprzedni_tryb_pracy = tryb_pracy;
		}


		// zadadnicze przetwarzanie w zależności od wybranego trybu pracy
		
		//ZADANIE 1 i 2
		lewy_wyjscie = lewy_wejscie;
		prawy_wyjscie = prawy_wejscie;
	
		mono_wejscie = TESTSIGNAL[index]; //tą linijkę się komentuje do zadania 4
		bufor[index] = mono_wejscie;
		
		bufor_okno[index] = mono_wejscie * okno[index];
		
		index++;
		
		if(index == NBUF)
		{
			rfft((DATA)&bufor, NBUF, SCALE);
			
			for(index = 0, int index_pomocniczy = 0; index < NBUF; index++, index_pomocniczy+=2)
			{
				realis = _smpy(bufor[index_pomocniczy], bufor[index_pomocniczy]);
				imaginalis = _smpy(bufor[index_pomocniczy+1], bufor[index_pomocniczy+1]);
				bufor[index] = realis + imaginalis;

				bufor_okno[index] = realis + imaginalis;
			}
			
			sqrt_16((DATA)&bufor, (DATA)&bufor, NBUF); 
			
			sqrt_16((DATA)&bufor_okno, (DATA)&bufor_okno, NBUF); 
		}
		if(index==NBUF)
		{
				index = 0; // tutaj breakpoint aby zrobić wykres bufora i bufor_okno
		}


		//ZADANIE 3 
		//szukamy maksimum	
		for(index = 0; index < NBUF; index++)
		{
			if(bufor[index] < bufor[index+1] && bufor[index] > progMinimalny) // prog minimalny do sprawdzenia
				maxIndex = index;
		}
		
		//obliczenie czestotliwosci
		czestotliwosc = (int)(max * 23) + ((14378 * (long)max) >>  15); // do przeanalizowania
		


		// zapisanie wartości na wyjście audio
		aic3204_codec_write(lewy_wyjscie, prawy_wyjscie);

	}


	// zakończenie pracy - zresetowanie kodeka
	aic3204_disable();
	oled_display_message("KONIEC PRACY       ", "                   ");
	while (1);
}
